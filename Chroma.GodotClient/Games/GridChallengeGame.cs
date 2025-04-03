using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Standard grid game where you avoid the red blocks and step on the blue targets.
/// </summary>
public partial class GridChallengeGame : GridGame
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        foreach (var level in ChallengeGames.Levels)
            for (int playerCount = 1; playerCount <= maxSupportedPlayers; playerCount++)
                yield return new(
                    playerCount,
                    GameSelection.GameType.Cooperative,
                    GameSelection.GameDifficulty.Regular,
                    "Challenge",
                    level.Name,
                    "Step on Blue. Avoid Red. Green is safe.",
                    GD.Load<Texture2D>("res://HowToPlay/Challenge.png"),
                    randomSeed => new GridChallengeGame(gridSpecs, level, 4f, randomSeed));
    }

    GameState _gameState;

    // One child Polygon2D corresponding to each object defined in _gameState.
    Node2D _safes = new() { Name = "Safes" };
    Node2D _dangers = new() { Name = "Dangers" };
    Node2D _targets = new() { Name = "Targets" };

    // Extra layers for animations.
    Node2D _aboveTargetLayer = new() { Name = "AboveTargetLayer" };
    Node2D _topLayer = new() { Name = "TopLayer" };

    // Movement speed (1/_gamePeriod).
    double _moveSpeed;
    // Seconds per game step.
    double _gamePeriod;
    // Seconds remaining of current step.
    double _timeout;
    // Last time a tile hit danger, to implement cooldown.
    ulong[,] _hitDangerTime;

    private enum States
    {
        // Initially everything is dark waiting for players to step into room.
        // We trigger the flourish upon the first step, then go to Countdown once the flourish finishes.
        Flourish,
        // Setup animation and countdown to game start.
        Countdown,
        // Main play loop.
        Play,
        // End of game animations and freeze.
        Win,
        Lose,
    };
    private States _state;
    public override string StateString => _state.ToString();

    protected override IReadOnlyList<Sounds> UsedSounds => [
        Sounds.Music_Challenge_8Bit,
        Sounds.Flourish_90sGameUi4,
        Sounds.Positive_90sGameUi6,
        Sounds.GameStart_8BitBlastE,
        Sounds.Positive_90sGameUi2,
        Sounds.Negative_8BitGame1,
        Sounds.GameWin_PowerUpSparkle1,
        Sounds.GameLose_90sGameUi15,
    ];

    public GridChallengeGame(ResolvedGridSpecs grid, IChallengeGameLevel level, double gameSpeed, int randomSeed) : base(grid)
    {
        GDPrint.LogState(level, gameSpeed, randomSeed);

        // Clamp to a reasonable maximum speed for now.
        // TODO: pass in unified game difficulty/speed settings.
        Assert.Clamp(ref gameSpeed, 0.2, 5.0);

        _moveSpeed = gameSpeed;
        _gamePeriod = _moveSpeed == 0 ? double.MaxValue : 1 / _moveSpeed;
        _timeout = _gamePeriod;

        InsideScreen!.Message.Show();
        InsideScreen!.LifeCounter.Show();
        InsideScreen!.LifeCounter.Update(5, 5);

        _gameState = new GameState(level.GetGameSpecs(grid));

        for (int i = 0; i < _gameState.Specs.Safe.Count; ++i)
        {
            var sb = _gameState.Specs.Safe[i];
            var s = MakePolygonArea(sb.Size, Colors.Green);
            s.Position = new Vector2(sb.Position.X, sb.Position.Y);
            s.Name = $"Safe_{i}";
            _safes.AddChild(s);
        }

        for (int i = 0; i < _gameState.Specs.Danger.Count; ++i)
        {
            var mb = _gameState.Specs.Danger[i];
            var dp = _gameState.DangerPositions[i];

            var d = MakePolygonArea(mb.Size, Colors.Red);
            d.Position = new Vector2(dp.Position.X, dp.Position.Y);
            d.Name = $"Danger_{i}";
            _dangers.AddChild(d);
        }

        _hitDangerTime = new ulong[GridSpecs.Width, GridSpecs.Height];
        // For each square eligible to be a target, randomly (but deterministically) decide whether it is.
        var rand = new Random(randomSeed);
        for (int x = 0; x < GridSpecs.Width; ++x)
        {
            for (int y = 0; y < GridSpecs.Height; ++y)
            {
                // Always pull next random, so it's deterministic only based on grid dimensions.
                var r = rand.NextSingle();
                if (_gameState.Specs.PotentialTargets[x, y] && r < level.ProportionTargets)
                {
                    var t = MakePolygonArea(new(1, 1), Colors.Blue);
                    t.Position = new Vector2(x, y);
                    t.Name = $"Target_{x}_{y}";
                    _targets.AddChild(t);
                }
            }
        }

        // Add in render order.
        AddChild(_targets);
        AddChild(_aboveTargetLayer);
        AddChild(_dangers);
        AddChild(_safes);
        AddChild(Flourish);
        AddChild(_topLayer);

        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _state = States.Flourish;
        _safes.Hide();
        _dangers.Hide();
        _targets.Hide();

        EnterFlourishState(EnterCountdown);
    }

    private void EnterCountdown()
    {
        _state = States.Countdown;
        InsideScreen!.Message.Text = "Get Ready";

        // Scale the time between countdown steps.
        float countdownIntervalSeconds = SkipIntro ? 0.1f : 0.4f;

        foreach (Polygon2D p in _safes.GetChildrenByType<Polygon2D>())
        {
            EaseInFlashWhite(p);
        }
        _safes.Show();
        AudioManager?.Play(Sounds.Positive_90sGameUi6);

        // Flash in some dangers on each count.
        const int CountdownIntervals = 5;
        for (int s = 0; s <= CountdownIntervals - 1; ++s)
        {
            var delay = countdownIntervalSeconds * (s + 1);
            for (int i = s; i < _dangers.GetChildCount(); i += CountdownIntervals)
            {
                foreach (Polygon2D p in _dangers.GetChild(i).GetChildrenByType<Polygon2D>())
                {
                    EaseInFlashWhite(p, delay);
                }
            }
            this.AddTimer(delay).Timeout += () => AudioManager?.Play(Sounds.Positive_90sGameUi6);
        }
        _dangers.Show();

        // As the game starts, flash in all targets, with some random timing offset.
        this.AddTimer(countdownIntervalSeconds * (CountdownIntervals + 1)).Timeout += () =>
        {
            var rand = new Random();
            foreach (Polygon2D p in _targets.GetChildrenByType<Polygon2D>())
            {
                EaseInFlashWhite(p, 0.2f * rand.NextSingle());
            }
            _targets.Show();
            EnterPlay();
        };
    }

    private void EnterPlay()
    {
        _state = States.Play;
        InsideScreen!.Message.Text = "Go!";

        AudioManager?.Play(Sounds.GameStart_8BitBlastE);
        AudioManager?.PlayMusic(Sounds.Music_Challenge_8Bit);
    }

    private void EnterWin(Vector2I point)
    {
        _state = States.Win;
        FlashSquare(_topLayer, point, Colors.Blue, Colors.White, flashTwice: false, freeAtEnd: false);
        Flourish.StartFromIndex(point, 2f, Colors.Blue);
        Flourish.Show();
        InsideScreen!.Message.Text = "You Win";
        AudioManager?.Play(Sounds.GameWin_PowerUpSparkle1);
        AudioManager?.StopMusic();
    }

    private void EnterLose(Vector2I point)
    {
        _state = States.Lose;
        FlashSquare(_topLayer, point, Colors.Red, Colors.Orange, flashTwice: true, freeAtEnd: false);
        Flourish.StartFromIndex(point, 2f, Colors.Red);
        Flourish.Show();
        InsideScreen!.Message.Text = "Game Over";
        AudioManager?.Play(Sounds.GameLose_90sGameUi15);
        AudioManager?.StopMusic();
    }

    public override void _Process(double delta)
    {
        if (_state == States.Flourish) { ProcessFlourish(); }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_state == States.Play) { ProcessPlay(delta); }
    }

    private void ProcessPlay(double delta)
    {
        // Check for presses since last time.
        var spaceState = GetViewport().World2D.DirectSpaceState;
        for (int x = 0; x < GridSpecs.Width; x++)
            for (int y = 0; y < GridSpecs.Height; y++)
            {
                const int cooldownMs = 1000;
                var ticks = Time.GetTicksMsec();
                if (!IsPressed(x, y) || (ticks - _hitDangerTime[x, y]) < cooldownMs) continue;

                // Check for collision at the center of this tile.
                var v = new Vector2(x + 0.5f, y + 0.5f);
                var result = spaceState.IntersectPoint(new()
                {
                    CollideWithAreas = true,
                    CollideWithBodies = false,
                    Position = v
                });
                // We're safe if any safe collider is overlapping.
                // Otherwise we hit danger if any danger is overlapping.
                // Otherwise if we hit a target, it scores.
                bool hitSafe = false;
                bool hitDanger = false;
                CollisionObject2D? hitTarget = null;
                foreach (var r in result)
                {
                    var collider = r["collider"].As<CollisionObject2D>();
                    var parent = collider.GetParent();
                    if (parent == _safes) { hitSafe = true; }
                    else if (parent == _dangers) { hitDanger = true; }
                    else if (!hitDanger && parent == _targets) { hitTarget = collider; }
                }
                var point = new Vector2I(x, y);
                if (!hitSafe)
                {
                    if (hitDanger)
                    {
                        var lc = InsideScreen!.LifeCounter;
                        lc.Decrement();
                        if (lc.CurrentLives == 0) { EnterLose(point); }
                        else
                        {
                            FlashSquare(_topLayer, point, Colors.Red, Colors.Orange, flashTwice: true);
                            AudioManager?.Play(Sounds.Negative_8BitGame1);
                            _hitDangerTime[x, y] = ticks;
                        }
                    }
                    else if (hitTarget != null)
                    {
                        hitTarget.QueueFree();
                        if (_targets.GetChildCount() == 1) { EnterWin(point); }
                        else
                        {
                            FlashSquare(_aboveTargetLayer, point, Colors.Blue, Colors.White, flashTwice: false);
                            AudioManager?.Play(Sounds.Positive_90sGameUi2);
                        }
                    }
                }
            }

        // Advance the target game state every _gamePeriod.
        _timeout += delta;
        if (_timeout > _gamePeriod)
        {
            _timeout -= _gamePeriod;
            _gameState.Step();

            // Set a tween over the next _gamePeriod to move objects into their target position.
            for (int i = 0; i < _gameState.DangerPositions.Count; ++i)
            {
                var p = _gameState.DangerPositions[i];
                var d = _dangers.GetChild<Node2D>(i);
                if (p.LastMovement == GameSpecs.BlockMovement.Teleporting)
                {
                    var polygon = d.GetChildByType<Polygon2D>();
                    if (polygon != null)
                    {
                        var t = FadeOut(polygon, new(), (float)_gamePeriod * 0.8f);
                        t.TweenCallback(Callable.From(() => polygon.Color = Colors.Red));
                        t.TweenCallback(Callable.From(() => d.Position = p.Position.ToVector2()));
                    }
                }
                else
                {
                    var t = d.CreateTween();
                    var setPosition = Callable.From((Vector2 position) => { d.Position = position; });
                    // Do the movement only in part of the _gamePeriod so that there's a clear pause at each step.
                    t.TweenInterval(0.25f * _gamePeriod);
                    t.TweenMethod(setPosition, d.Position, p.Position.ToVector2(), 0.5f * _gamePeriod);
                }
            }
        }
    }
}
