using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Games.Collect;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Competitive grid game where players race to collect targets.
///
/// TODO: make flourish closer to color override, need inverse color->HSL function.
/// TODO: make flourish transparent/additive
/// </summary>
public partial class GridCollectGame : GridGame
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        var maxPlayers = int.Min(maxSupportedPlayers, MaxPlayerCount(gridSpecs));
        for (int playerCount = 2; playerCount <= maxPlayers; playerCount++)
        {
            int playerCountCapture = playerCount;
            yield return new(
                playerCount,
                GameSelection.GameType.Competitive,
                GameSelection.GameDifficulty.Regular,
                "CollectPvp",
                null,
                "Be the first to collect all bright tiles in your area.",
                GD.Load<Texture2D>("res://HowToPlay/Collect.png"),
                randomSeed => new GridCollectGame(gridSpecs, playerCountCapture, randomSeed));
        }

        for (int playerCount = 1; playerCount <= maxSupportedPlayers; playerCount++)
            yield return new(
                playerCount,
                GameSelection.GameType.Cooperative,
                GameSelection.GameDifficulty.Regular,
                "CollectCoop",
                null,
                "How fast can you collect all the bright tiles?",
                GD.Load<Texture2D>("res://HowToPlay/CollectCoop.png"),
                randomSeed => new GridCollectGame(gridSpecs, 1, randomSeed));
    }

    // Containers/layers for game objects.
    Node2D _playerAreaIndicators = new() { Name = "PlayerAreaIndicators" };
    Node2D _targets = new() { Name = "Targets" };
    Node2D _scoreIndicators = new() { Name = "ScoreIndicators" };

    // Layers for animations.
    Node2D _topLayer = new() { Name = "TopLayer" };
    Node2D _textLayer = new() { Name = "TextLayer" };
    Node2D _playerFlourishLayer = new() { Name = "PlayerFlourishLayer" };
    Node2D _bottomLayer = new() { Name = "BottomLayer" };

    int _playerCount;
    PlayerAreas _playerAreas;
    CollectProgress _collectProgress;
    // [_playerCount][_playerWidth] of score indicators for tiles collected in each column.
    Polygon2D[][] _scoreByPlayerColumn;
    // [_playerCount][_playerWidth, _playerHeight] of current targets in each player area.
    Polygon2D?[][,] _currentTargetsByPlayer;
    Dictionary<ulong, List<int>> _playerFinishTick = [];

    private static Color ScoreIndicatorColor(int player) => PlayerColors[player].Darkened(0.4f);
    private static Color DimPlayerColor(int player) => PlayerColors[player].Darkened(0.8f);

    private enum States
    {
        Flourish,
        SelectColor,
        Countdown,
        Play,
        GameOver,
    };
    private States _state;
    public override string StateString => _state.ToString();

    public bool QuickCollectForDebug { get; set; }

    const int MinimumPlayerWidth = 3;
    public static int MaxPlayerCount(ResolvedGridSpecs grid) => grid.Width / MinimumPlayerWidth;

    protected override IReadOnlyList<Sounds> UsedSounds => [
        Sounds.Countdown_CountdownSoundEffect8Bit,
        Sounds.Flourish_8BitBlastK,
        Sounds.Flourish_90sGameUi4,
        Sounds.Flourish_CuteLevelUp3,
        Sounds.GameStart_8BitBlastE,
        Sounds.Music_Competitive_Ragtime,
        Sounds.Neutral_Button,
        Sounds.Positive_90sGameUi2,
    ];

    public GridCollectGame(ResolvedGridSpecs grid, int playerCount, int randomSeed) : base(grid)
    {
        GDPrint.LogState(playerCount, randomSeed);

        int maxPlayerCount = MaxPlayerCount(grid);
        Assert.Clamp(ref playerCount, 1, maxPlayerCount);

        InsideScreen!.Message.Show();

        _playerCount = playerCount;
        _playerAreas = PlayerAreas.SplitHorizontally(GridSpecs.Width, GridSpecs.Height, playerCount);
        var playerWidth = _playerAreas.PlayerAreaSize.Width;
        var playerHeight = _playerAreas.PlayerAreaSize.Height;

        _collectProgress = new CollectProgress(playerCount, _playerAreas.PlayerAreaSize, new Random(randomSeed));
        _scoreByPlayerColumn = new Polygon2D[_playerCount][];
        _currentTargetsByPlayer = new Polygon2D[_playerCount][,];
        for (int i = 0; i < _playerCount; ++i)
        {
            _scoreByPlayerColumn[i] = new Polygon2D[playerWidth];
            _currentTargetsByPlayer[i] = new Polygon2D[playerWidth, playerHeight];
            for (int w = 0; w < playerWidth; ++w)
            {
                var p = MakePolygon(new(0, 0), new(1, playerHeight), ScoreIndicatorColor(i));
                p.Position = new(_playerAreas.Get(i).X + w, GridSpecs.Height);
                _scoreByPlayerColumn[i][w] = p;
                _scoreIndicators.AddChild(p);
            }
            _playerFlourishLayer.AddChild(new Flourish(new(playerWidth, playerHeight), new(playerWidth, playerHeight))
            {
                Position = _playerAreas.Get(i).ToVector2(),
                Visible = false,
            });

            if (_playerCount == 1) continue;

            // Center the font in the player area.
            // Move it left so that the glyph is centred (it's a fixed 3x6 font).
            // Move it up so that the top of the glyph is at the origin.
            var labelOffset = new Vector2(_playerAreas.PlayerAreaSize.Width / 2 - 1, -5);
            var label = new Label()
            {
                Position = _playerAreas.Get(i).ToVector2() + labelOffset,
                HorizontalAlignment = HorizontalAlignment.Center,
                LabelSettings = new LabelSettings()
                {
                    Font = GD.Load<Font>("res://Fonts/m3x6.ttf"),
                    FontSize = 16,
                    FontColor = new(),
                },
            };
            _textLayer.AddChild(label);
        }
        _textLayer.Hide();

        // Add in render order.
        AddChild(_playerAreaIndicators);
        AddChild(_bottomLayer);
        AddChild(_scoreIndicators);
        AddChild(_targets);
        AddChild(Flourish);
        AddChild(_playerFlourishLayer);
        AddChild(_textLayer);
        AddChild(_topLayer);

        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _state = States.Flourish;
        EnterFlourishState(EnterSelectColor);
    }

    private void EnterSelectColor()
    {
        _state = States.SelectColor;
        InsideScreen!.Message.Text = _playerCount > 1 ? "Choose a colour" : "";

        var revealTime = SkipIntro ? 0.1f : 1f;
        for (int i = 0; i < _playerCount; ++i)
        {
            var p = MakePolygon(new(), _playerAreas.PlayerAreaSize.ToVector2(), DimPlayerColor(i));
            p.Position = _playerAreas.Get(i).ToVector2();
            _playerAreaIndicators.AddChild(p);
            var delay = (i + 1) * revealTime / _playerCount;
            FlashColor(p, new(), ScoreIndicatorColor(i), p.Color, delay);
            this.AddTimer(delay).Timeout += () => AudioManager?.Play(Sounds.Positive_90sGameUi2);
        }
    }

    private void ProcessSelectColor()
    {
        bool someSelected = false;
        foreach (var (x, y) in TakePressed())
        {
            var (pi, relX, _) = _playerAreas.GetPlayerRelative(x, y);
            if (pi == -1) { continue; }

            foreach (var p in _playerAreaIndicators.GetChildrenByType<Polygon2D>())
            {
                if ((int)p.Position.X == _playerAreas.Get(pi).X && p.Color == DimPlayerColor(pi))
                {
                    FadeOut(p, ScoreIndicatorColor(pi), 0.2f);
                    someSelected = true;
                }
            }
        }

        bool allSelected = true;
        foreach (var p in _playerAreaIndicators.GetChildrenByType<Polygon2D>())
        {
            var (pi, _, _) = _playerAreas.GetPlayerRelative((int)p.Position.X, (int)p.Position.Y);
            allSelected &= p.Color == ScoreIndicatorColor(pi);
        }
        if (allSelected || SkipIntro)
        {
            foreach (var p in _playerAreaIndicators.GetChildrenByType<Polygon2D>())
            {
                var (pi, _, _) = _playerAreas.GetPlayerRelative((int)p.Position.X, (int)p.Position.Y);
                FadeOut(p, DimPlayerColor(pi));
            }
            EnterCountdown();
        }

        if (someSelected)
        {
            if (allSelected)
                AudioManager?.Play(Sounds.GameStart_8BitBlastE);
            else
                AudioManager?.Play(Sounds.Flourish_CuteLevelUp3);
        }
    }

    private void SpawnInitialTargets()
    {
        var startingTargets = _playerAreas.PlayerAreaSize.Width;
        var delay = 1f / startingTargets;
        for (int column = 0; column < startingTargets; ++column)
        {
            var currentDelay = 0.1f + column * delay;
            for (int player = 0; player < _playerCount; ++player)
            {
                SpawnNextTarget(player, column, currentDelay);
            }
            this.AddTimer(currentDelay).Timeout += () => AudioManager?.Play(Sounds.Positive_90sGameUi2);
        }
    }

    private void EnterCountdown()
    {
        _state = States.Countdown;

        // Scale the time between countdown steps.
        float countdownIntervalSeconds = SkipIntro ? 0.1f : 1f;

        this.AddTimer(2 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "3";
            AudioManager?.Play(Sounds.Countdown_CountdownSoundEffect8Bit);
        };
        this.AddTimer(3 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "2";
        };
        this.AddTimer(4 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "1";
        };
        this.AddTimer(5 * countdownIntervalSeconds).Timeout += () =>
        {
            InsideScreen!.Message.Text = "Go!";
            SpawnInitialTargets();
            EnterPlay();
        };
    }

    private void EnterPlay()
    {
        _state = States.Play;
        AudioManager?.PlayMusic(Sounds.Music_Competitive_Ragtime);
        // Clear any existing input.
        TakePressed();
    }

    private void SpawnNextTarget(int player, int column, double fadeInTime)
    {
        if (_collectProgress.AllTargetsTaken(player, column)) { return; }

        var nt = _collectProgress.TakeNextTarget(player, column);
        // Flip because we're moving the score indicator up from below.
        nt.Y = _playerAreas.PlayerAreaSize.Height - nt.Y - 1;
        var t = MakePolygon(new(0, 0), new(1, 1), Colors.White);
        t.Position = _playerAreas.Get(player).ToVector2() + nt.ToVector2();
        _currentTargetsByPlayer[player][nt.X, nt.Y] = t;
        _targets.AddChild(t);
        EaseInFlashWhite(t, fadeInTime);
    }

    private void ProcessPlay(double delta)
    {
        // Check for presses since last time.
        var spaceState = GetViewport().World2D.DirectSpaceState;
        foreach (var (x, y) in TakePressed())
        {
            var (pi, rx, ry) = _playerAreas.GetPlayerRelative(x, y);
            if (pi == -1) { continue; }

            var tile = _currentTargetsByPlayer[pi][rx, ry];
            if (tile == null) { continue; }

            _currentTargetsByPlayer[pi][rx, ry] = null;

            // Slide the tile toward the top of the score indicator.
            var scoreIndicator = _scoreByPlayerColumn[pi][rx];
            var newPosition = scoreIndicator.Position + new Vector2(0, -1);
            const float MoveSpeed = 20;
            var timeframe = Math.Abs(newPosition.Y - tile.Position.Y) / MoveSpeed;
            var tween = tile.CreateTween();
            tween.TweenMethod(Callable.From((Vector2 p) => tile.Position = p), tile.Position, newPosition, timeframe);
            tween.TweenMethod(Callable.From((Color c) => tile.Color = c), tile.Color, ScoreIndicatorColor(pi), 0.3);
            tween.TweenCallback(Callable.From(tile.QueueFree));
            tween.TweenCallback(Callable.From(() => scoreIndicator.Position = newPosition));
            _collectProgress.ScorePlayerColumn(pi, rx);

            if (QuickCollectForDebug)
            {
                for (int i = 0; i < _playerAreas.PlayerAreaSize.Width; i++)
                {
                    while (!_collectProgress.AllTargetsTaken(pi, i))
                    {
                        _collectProgress.TakeNextTarget(pi, i);
                        _collectProgress.ScorePlayerColumn(pi, i);
                    }
                }
            }

            if (_collectProgress.AllTargetsScored(pi))
            {
                var pf = _playerFlourishLayer.GetChild<Flourish>(pi);
                if (!pf.Visible)
                {
                    pf.Show();
                    pf.StartFromIndex(new(rx, ry), 1.5f, PlayerColors[pi]);
                    _playerFinishTick.GetOrNew(Time.GetTicksMsec(), out var tick);
                    tick.Add(pi);
                    AudioManager?.Play(Sounds.Flourish_8BitBlastK);
                }
            }
            else
            {
                SpawnNextTarget(pi, rx, 0.3);
                AudioManager?.Play(Sounds.Positive_90sGameUi2);
            }
        }

        bool gameOver = true;
        for (int i = 0; i < _playerCount; ++i) { gameOver &= _collectProgress.AllTargetsScored(i); }
        if (gameOver) { EnterGameOver(); }
    }

    private void EnterGameOver()
    {
        _state = States.GameOver;
        AudioManager?.StopMusic();
        int i = 0;
        float delay = 0.5f;
        // Since the font is 6 high, we don't want it to be cropped.
        float offset = float.Max(0, _playerAreas.PlayerAreaSize.Height - 6) / (_playerCount - 1);
        foreach (var tick in _playerFinishTick.Values)
        {
            foreach (var pi in tick)
            {
                var label = _textLayer.GetChild<Label>(pi);
                label.Text = $"{i + 1}";
                label.Position = label.Position + new Vector2(0, float.Ceiling(i * offset));
                this.AddTimer(2f + i * delay).Timeout += () =>
                {
                    var t = label.CreateTween();
                    var setColor = Callable.From((Color c) => label.LabelSettings.FontColor = c);
                    t.TweenMethod(setColor, new Color(), Colors.White, 0.2f);
                    t.TweenMethod(setColor, Colors.White, PlayerColors[pi], 0.3f);
                    AudioManager?.Play(Sounds.Neutral_Button);
                };
                i++;
            }
        }
        _textLayer.Show();
    }

    public override void _Process(double delta)
    {
        if (_state == States.Flourish) { ProcessFlourish(); }
        else if (_state == States.SelectColor) { ProcessSelectColor(); }
        else if (_state == States.Play) { ProcessPlay(delta); }
    }
}
