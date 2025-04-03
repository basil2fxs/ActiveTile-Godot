using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System.Collections.Generic;
using System.Linq;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Competitive grid game where two sides shoot at each other.
///
/// TODO: some way to specify that games should be rotated 90deg, needs to be done in gridspecs probably.
/// </summary>
public partial class GridBattleGame : GridGame
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        for (int playerCount = 2; playerCount <= maxSupportedPlayers; playerCount++)
            yield return new(
                playerCount,
                GameSelection.GameType.Competitive,
                GameSelection.GameDifficulty.Regular,
                "Battle",
                null,
                "Shoot your opponents guns before they shoot yours! You only have 5 bullets at a time.",
                GD.Load<Texture2D>("res://HowToPlay/Battle.png"),
                randomSeed => new GridBattleGame(gridSpecs, 0.5f));
    }

    // Containers/layers for game objects.
    Node2D _homeRows = new() { Name = "HomeRows" };
    Node2D _guns = new() { Name = "Guns" };
    Node2D _magazines = new() { Name = "Magazines" };
    Node2D _bullets = new() { Name = "Bullets" };

    // Layers for animations.
    Node2D _topLayer = new() { Name = "TopLayer" };
    Node2D _bottomLayer = new() { Name = "BottomLayer" };

    // Time for bullet to move one unit.
    double _gamePeriod;
    // Player ammo.
    int[] _ammo;
    // Max ammo.
    int _magSize;
    // Delay after shooting for each gun, by player and x coordinate.
    ulong _cooldownMsec;
    ulong[][] _shotTime;
    // Whether each column has only guns of a single player.
    bool[] _gameOver;

    private enum States
    {
        // Initially everything is dark waiting for players to step into room.
        // We trigger the flourish upon the first step, then go to Countdown once the flourish finishes.
        Flourish,
        // Wait for players to take their places.
        SelectColor,
        // Setup animation and countdown to game start.
        Countdown,
        // Main play loop.
        Play,
        // End of game animations and freeze.
        GameOver,
    };
    private States _state;
    public override string StateString => _state.ToString();

    protected override IReadOnlyList<Sounds> UsedSounds => [
        Sounds.Collision_CardboardboxA,
        Sounds.Collision_CardboardboxF,
        Sounds.Collision_T8DrumLoopE,
        Sounds.Countdown_CountdownSoundEffect8Bit,
        Sounds.Flourish_90sGameUi4,
        Sounds.Flourish_CuteLevelUp3,
        Sounds.Music_Competitive_StealthBattle,
        Sounds.Negative_HurtC08,
        Sounds.Negative_StabF01,
        Sounds.Neutral_8BitBlastA,
        Sounds.Neutral_Button,
        Sounds.Positive_90sGameUi6,
    ];

    private static Color DimPlayerColor(int player) => PlayerColors[player].Darkened(0.8f);
    private static Color GunColor => Colors.Orange;
    private static Color BulletColor => Colors.White;

    public GridBattleGame(ResolvedGridSpecs grid, double gamePeriod) : base(grid)
    {
        GDPrint.LogState(gamePeriod);

        // Clamp to a reasonable maximum speed for now.
        // TODO: pass in unified game difficulty/speed settings.
        Assert.Clamp(ref gamePeriod, 0.2, 5.0);

        InsideScreen!.Message.Show();

        _gamePeriod = gamePeriod;
        _magSize = 5;
        _ammo = [_magSize, _magSize];
        _shotTime = [new ulong[GridSpecs.Width], new ulong[GridSpecs.Width]];
        // Cooldown should be at least the time it takes the bullet to move to the next square.
        _cooldownMsec = (ulong)(1000 * _gamePeriod);
        _gameOver = new bool[GridSpecs.Width - 1];

        // Game is split into two symmetric sides mirrored at grid.Width / 2.
        for (int player = 0; player < 2; player++)
        {
            const int homeSize = 2;
            var h = MakePolygonArea(new(GridSpecs.Width - 1, homeSize), DimPlayerColor(player));
            h.Position = new(0, YForPlayer(player, 0, homeSize));
            h.Name = $"Home_{player}";
            h.AddChild(new PlayerIndex(player));
            _homeRows.AddChild(h);
            for (var x = 0; x < GridSpecs.Width - 1; ++x)
            {
                MakeGun(player, x, x % 2);
            }
            for (int i = 0; i < _magSize; ++i)
            {
                var m = MakePolygon(new(), new(1, 1), BulletColor);
                m.Position = new(GridSpecs.Width - 1, YForPlayer(player, i));
                m.Name = $"Ammo_{player}_{i}";
                m.AddChild(new PlayerIndex(player));
                _magazines.AddChild(m);
            }
        }

        // Add in render order.
        AddChild(_bottomLayer);
        AddChild(_homeRows);
        AddChild(_guns);
        AddChild(_magazines);
        AddChild(_bullets);
        AddChild(Flourish);
        AddChild(_topLayer);

        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _state = States.Flourish;
        _homeRows.Hide();
        _guns.Hide();
        _magazines.Hide();
        _bullets.Hide();
        EnterFlourishState(EnterSelectColor);
    }

    private void EnterSelectColor()
    {
        _state = States.SelectColor;
        InsideScreen!.Message.Text = "Choose a side";

        foreach (Polygon2D p in _homeRows.GetChildrenByType<Polygon2D>())
        {
            EaseInFlashWhite(p);
        }
        _homeRows.Show();

        AudioManager?.Play(Sounds.Positive_90sGameUi6);
    }

    private void EnterCountdown()
    {
        _state = States.Countdown;
        InsideScreen!.Message.Text = "Get Ready";

        // Scale the time between countdown steps.
        float countdownIntervalSeconds = SkipIntro ? 0.1f : 1f;
        SortedSet<double> soundTimes = [];

        foreach (Node2D g in _guns.GetChildren())
        {
            var delay = (int)g.Position.X % 2 == 0 ? countdownIntervalSeconds : 2 * countdownIntervalSeconds;
            soundTimes.Add(delay);
            foreach (var p in g.GetChildrenByType<Polygon2D>())
            {
                EaseInFlashWhite(p, delay);
            }
        }
        _guns.Show();

        foreach (var p in _magazines.GetChildrenByType<Polygon2D>())
        {
            var (_, i) = PlayerAndRelativeY(p);
            var delay = (3 + i / _magSize) * countdownIntervalSeconds;
            soundTimes.Add(delay);
            EaseInFlashWhite(p, delay);
        }
        _magazines.Show();
        _bullets.Show();

        foreach (var delay in soundTimes)
            this.AddTimer(delay).Timeout += () => AudioManager?.Play(Sounds.Positive_90sGameUi6);

        this.AddTimer(5 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "3";
            if (!SkipIntro)
            {
                var playback = AudioManager?.Play(Sounds.Countdown_CountdownSoundEffect8Bit);
                if (playback.HasValue)
                {
                    // We don't want the last sound, will play a different one.
                    this.AddTimer(2.5f * countdownIntervalSeconds).Timeout += () => AudioManager?.Stop(playback.Value);
                }
            }
        };
        this.AddTimer(6 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "2";
        };
        this.AddTimer(7 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "1";
        };
        this.AddTimer(8 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "Go!";
            AudioManager?.Play(Sounds.Negative_StabF01);
            EnterPlay();
        };
    }

    private void EnterPlay()
    {
        _state = States.Play;
        AudioManager?.PlayMusic(Sounds.Music_Competitive_StealthBattle, fadeTime: 0.2);
        // Ignore any presses so far.
        TakePressed();
    }

    private void EnterGameOver()
    {
        _state = States.GameOver;
        foreach (Polygon2D p in _homeRows.GetChildrenByType<Polygon2D>()) { FadeOut(p); }
        foreach (Polygon2D p in _magazines.GetChildrenByType<Polygon2D>()) { FadeOut(p); }
        foreach (Polygon2D p in _bullets.GetChildrenByType<Polygon2D>()) { FadeOut(p); }

        AudioManager?.StopMusic();
        AudioManager?.Play(Sounds.Neutral_8BitBlastA);
        // Playing audio at low pitch scale dilates them so they need to be played a bit earlier
        // so that they don't sound delayed.
        const double audioLeadTime = 0.2;

        SortedList<float, Polygon2D>[] gunsLeft = [[], []];
        foreach (Area2D g in _guns.GetChildrenByType<Area2D>(recursive: false))
        {
            if (g.IsQueuedForDeletion()) continue;
            var p = g.GetChildByType<Polygon2D>();
            var pi = PlayerIndex.Of(g);
            if (!Assert.Clamp(ref pi, 0, 1)) continue;
            if (p != null) gunsLeft[pi].Add(g.Position.X, p);
        }
        var (loser, winner) = gunsLeft[0].Count < gunsLeft[1].Count ? (gunsLeft[0], gunsLeft[1]) : (gunsLeft[1], gunsLeft[0]);
        double initialDelay = 1.5;
        double loserCountTime = 1 + 0.1 * loser.Count;
        for (int i = 0; i < loser.Count; i++)
        {
            var index = i;
            var timeProportion = (float)i / loser.Count;
            var time = initialDelay + timeProportion * loserCountTime;
            this.AddTimer(time).Timeout += () =>
            {
                FlashColor(loser.Values[index], loser.Values[index].Color, Colors.White, GunColor);
                FlashColor(winner.Values[index], winner.Values[index].Color, Colors.White, GunColor);
            };
            var pitchScale = float.Lerp(0.5f, 1, timeProportion);
            this.AddTimer(time - audioLeadTime / pitchScale).Timeout += () =>
                AudioManager?.Play(Sounds.Neutral_Button, pitchScale);
        }
        int winnerExcess = winner.Count - loser.Count;
        double winnerCountTime = 1 + 0.1 * winnerExcess;
        for (int i = 0; i < winnerExcess; i++)
        {
            var index = loser.Count + i;
            var timeProportion = ((float)i + 1) / winnerExcess;
            var time = initialDelay + loserCountTime + timeProportion * winnerCountTime;
            this.AddTimer(time).Timeout += () =>
            {
                FlashColor(winner.Values[index], winner.Values[index].Color, Colors.White, GunColor);
            };
            var pitchScale = float.Lerp(1, 2, timeProportion);
            this.AddTimer(time - audioLeadTime / pitchScale).Timeout += () =>
                AudioManager?.Play(Sounds.Neutral_Button, pitchScale);
        }
    }

    public override void _Process(double delta)
    {
        if (_state == States.Flourish) { ProcessFlourish(); }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_state == States.Play) { ProcessPlay(delta); }
        else if (_state == States.SelectColor) { ProcessSelectColor(delta); }
    }

    private void ProcessSelectColor(double delta)
    {
        bool someSelected = false;
        var spaceState = GetViewport().World2D.DirectSpaceState;
        foreach (var (x, y) in TakePressed())
        {
            // Check for collision at the center of this tile.
            var v = new Vector2(x + 0.5f, y + 0.5f);
            var result = spaceState.IntersectPoint(new()
            {
                CollideWithAreas = true,
                CollideWithBodies = false,
                Position = v
            });

            foreach (var r in result)
            {
                var collider = r["collider"].As<CollisionObject2D>();
                var parent = collider.GetParent();
                if (parent != _homeRows) continue;

                var pi = PlayerIndex.Of(collider);
                if (!Assert.Clamp(ref pi, 0, 1, nameof(pi))) continue;
                var p = collider.GetChildByType<Polygon2D>();
                if (p == null || p.Color.V > DimPlayerColor(pi).V) continue;
                someSelected = true;
                FadeOut(p, PlayerColors[pi], 0.2f);
            }
        }

        if (someSelected)
            AudioManager?.Play(Sounds.Flourish_CuteLevelUp3);

        if (_homeRows.GetChildrenByType<Polygon2D>().Select((p, i) => p.Color == PlayerColors[i]).All(p => p))
            EnterCountdown();
    }

    /// <summary>
    /// Free a bullet node and return one ammo to that player.
    /// </summary>
    private void ReturnBullet(Area2D bullet)
    {
        var pi = PlayerIndex.Of(bullet);
        if (!Assert.Clamp(ref pi, 0, 1)) return;

        _ammo[pi] += 1;
        UpdateAmmo();
        bullet.QueueFree();
        FlashRect(_topLayer, bullet.Position + 0.5f * Vector2.One, 1.8f * Vector2.One, 0.5f * BulletColor, 0.5f * Colors.Pink, flashTwice: false);
    }

    /// <summary>
    /// Update the ammo indicators.
    /// </summary>
    private void UpdateAmmo()
    {
        foreach (var p in _magazines.GetChildrenByType<Polygon2D>())
        {
            var (pi, bi) = PlayerAndRelativeY(p);
            if (!Assert.Clamp(ref pi, 0, 1)) continue;

            p.Visible = _ammo[pi] > bi;
        }
    }

    private void ProcessPlay(double delta)
    {
        // First check if any bullets have hit anything.
        bool hit = false;
        foreach (Area2D b in _bullets.GetChildren())
        {
            foreach (var other in b.GetOverlappingAreas())
            {
                if (PlayerIndex.Of(b) == PlayerIndex.Of(other)) { continue; }

                hit = true;

                var otherParent = other.GetParent();
                if (otherParent == _guns)
                {
                    // Damage or destroy the gun by freeing the existing one and putting
                    // a smaller one there. If it's already small, mark that column as done.
                    other.QueueFree();
                    int x = GridSpecs.ToGridX(other.Position.X);
                    var (pi, ry) = PlayerAndRelativeY(other);
                    if ((int)ry == 3) MakeGun(pi, x, 0);
                    else _gameOver[x] = true;
                    ReturnBullet(b);

                    // Animate.
                    var otherCoordinate = new Vector2I(x, (int)other.Position.Y);
                    FlashSquare(_topLayer, otherCoordinate, GunColor, Colors.Red, flashTwice: true);
                    FlashRectI(_bottomLayer, new(x, 0), new(1, GridSpecs.Height), Colors.Black, Colors.DarkRed, flashTwice: false);

                    AudioManager?.Play(Sounds.Negative_HurtC08);
                }
                if (otherParent == _homeRows || otherParent == _bullets)
                {
                    // Bullets cancel each other.
                    ReturnBullet(b);
                    AudioManager?.Play(Sounds.Collision_T8DrumLoopE);
                }
            }
        }
        if (hit) UpdateGunColors();

        // Check for presses since last time.
        var spaceState = GetViewport().World2D.DirectSpaceState;
        foreach (var (x, y) in TakePressed())
        {
            // Check for collision at the center of this tile.
            var v = new Vector2(x + 0.5f, y + 0.5f);
            var result = spaceState.IntersectPoint(new()
            {
                CollideWithAreas = true,
                CollideWithBodies = false,
                Position = v
            });

            ulong now = Time.GetTicksMsec();
            foreach (var r in result)
            {
                var collider = r["collider"].As<CollisionObject2D>();
                var parent = collider.GetParent();
                var pi = PlayerIndex.Of(collider);
                if (!Assert.Clamp(ref pi, 0, 1)) continue;

                // For now we only care about firing guns.
                bool notGun = parent != _guns;
                bool gunDestroyed = collider.IsQueuedForDeletion();
                bool columnWon = _gameOver[GridSpecs.ToGridX(collider.Position.X)];
                bool noAmmo = _ammo[pi] <= 0;
                bool justFired = now - _shotTime[pi][x] < _cooldownMsec;
                if (notGun || gunDestroyed || columnWon || justFired)
                {
                    continue;
                }
                if (noAmmo)
                {
                    AudioManager?.Play(Sounds.Collision_CardboardboxF);
                    continue;
                }
                _ammo[pi] -= 1;
                _shotTime[pi][x] = now;
                UpdateAmmo();

                // Create the bullet.
                var b = MakePolygonArea(new(1, 1), BulletColor);
                var direction = (pi % 2 == 0) ? Vector2.Down : Vector2.Up;
                b.Position = new Vector2(x, collider.Position.Y) + direction;
                b.AddChild(new PlayerIndex(pi));
                // We want the bullets to be aligned to the grid most of the time because they appear clearer,
                // but still move smoothly enough to not skip over opposing bullets.
                // Use a repeating timer to quickly tween it forward every _gamePeriod.
                var t = new Timer() { Autostart = true, OneShot = false, WaitTime = _gamePeriod };
                t.Timeout += () =>
                {
                    var method = Callable.From((Vector2 p) => { b.Position = p; });
                    b.CreateTween().TweenMethod(method, b.Position, b.Position + direction, 0.1f * _gamePeriod);
                };
                b.AddChild(t);
                _bullets.AddChild(b);
                AudioManager?.Play(Sounds.Collision_CardboardboxA);

                // Animate.
                foreach (Polygon2D p in collider.GetChildrenByType<Polygon2D>())
                {
                    FlashColor(p, p.Color, Colors.White, p.Color);
                }
            }
        }

        // Check for game over, i.e. each column only has one player's guns.
        bool gameOver = true;
        foreach (bool column in _gameOver) { gameOver &= column; }
        if (gameOver) { EnterGameOver(); }
    }

    private void UpdateGunColors()
    {
        foreach (Area2D g in _guns.GetChildrenByType<Area2D>(recursive: false))
        {
            if (_gameOver[GridSpecs.ToGridX(g.Position.X)])
            {
                var p = g.GetChildByType<Polygon2D>();
                if (p != null) p.Color = GunColor.Darkened(0.5f);
            }
        }
    }

    /// <summary>
    /// A Y position given a relative position from a player.
    /// </summary>
    /// <param name="player">0 or 1.</param>
    /// <param name="y">Units from that player's side of the grid.</param>
    /// <param name="size">Size of block to mirror.</param>
    /// <returns>Absolute Y position on the grid.</returns>
    private int YForPlayer(int player, int y, int size = 1) => player == 0 ? y : GridSpecs.Height - y - size;

    /// <summary>
    /// Gets a node's player index and its relative Y position with respect to that player.
    /// </summary>
    private (int, float) PlayerAndRelativeY(Node2D p)
    {
        int pi = PlayerIndex.Of(p);
        float y = pi == 0 ? p.Position.Y : GridSpecs.Height - 1 - p.Position.Y;
        return (pi, y);
    }

    private void MakeGun(int player, int x, int odd)
    {
        if (!Assert.Clamp(ref player, 0, 1)) return;

        // Odd guns have size 2.
        // Their origin is the square closest to the opponent.
        // This allows spawning bullets one square from the origin
        // and determining the gun's size by checking its Position.X.
        var g = MakePolygonArea(new(0, -odd * (1 - player)), new(1, 1 + odd * player), GunColor);
        g.Position = new(x, YForPlayer(player, 2 + odd));
        g.Name = $"Gun_{player}_{x}_{odd}";
        g.AddChild(new PlayerIndex(player));
        _guns.AddChild(g);
    }
}
