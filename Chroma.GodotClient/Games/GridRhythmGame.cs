using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Games.Collect;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.GameLogic.Physics;
using Color = Godot.Color;

namespace MysticClue.Chroma.GodotClient.Games;

// TODO - currently, the timing of the targets are eyeballed to match the music. Look into the MIDIPlayer node package
/// <summary>
/// Competitive rhythm game where target tiles approach the players which closely synchronizes with the music.
/// Players are then required to step on the moving tiles, with some timing tolerance.
/// </summary>
public partial class GridRhythmGame : GridGame
{
    private readonly Random _rand;

    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        var maxPlayers = int.Min(maxSupportedPlayers, MaxPlayerCount(gridSpecs));
        for (int playerCount = 1; playerCount <= maxPlayers; playerCount++)
        {
            int playerCountCapture = playerCount;
            yield return new(
                playerCount,
                GameSelection.GameType.Competitive,
                GameSelection.GameDifficulty.Regular,
                "Rhythm",
                null,
                "Step on the tiles as they approach you. You get more points with more precise timing.",
                GD.Load<Texture2D>("res://HowToPlay/Rhythm.png"),
                randomSeed => new GridRhythmGame(gridSpecs, playerCountCapture, randomSeed));
        }

        // TODO - need to develop further to account for coop
        // yield return new(
        //     1,
        //     GameSelection.GameType.Cooperative,
        //     GameSelection.GameDifficulty.Regular,
        //     "Rhythm",
        //     null,
        //     "Step on the tiles as they approach you. You get more points with more precise timing.",
        //     GD.Load<Texture2D>("res://HowToPlay/Rhythm.png"),
        //     randomSeed => new GridRhythmGame(gridSpecs, 1, randomSeed));
    }

    // Containers/layers for game objects.
    Node2D _playerPlatformSegmentNode = new() { Name = "PlayerPlatformSegmentNode" };
    Node2D _playerPlatformSelectionNode = new() { Name = "PlayerPlatformSelectionNode" };
    Node2D _playerTargetAreaNode = new() { Name = "PlayerTargetAreaNode" };
    Node2D _playerColumnNode = new() { Name = "PlayerColumnNode" };
    Node2D _playerDespawnIndicatorNode = new() { Name = "PlayerDespawnIndicatorNode" };
    Node2D _playerRankIndicatorNode = new() { Name = "PlayerRankIndicatorNode" };
    Node2D _playerTargetNode = new() { Name = "PlayerTargetNode" };

    // Layers for animations.
    Node2D _playerFlourishLayer = new() { Name = "PlayerFlourishLayer" };

    // Player separated areas
    PlayerAreas _playerPlatformSelectionAreas;
    PlayerAreas _playerTargetAreas;

    int _playerCount;
    private int _playerAreaWidth;
    private const int PlatformHeight = 3;

    private Queue<Polygon2D>[,] _playerTargets;
    private Polygon2D[] _playerAreas;
    private Polygon2D[,] _playerColumns;
    private Polygon2D[,] _playerPlatformSegments;
    private Polygon2D[] _playerPlatforms;
    private Polygon2D[,] _playerDespawnIndicators;
    private int[] _playerScores;
    private int _randomSeed;
    private double _gameTimeElapsed;

    private enum States
    {
        Flourish,
        SelectColor,
        Countdown,
        Play,
        GameOver,
    }

    private States _state;

    const int MinimumPlayerWidth = 3;
    private const int MaximumPlayerWidth = 4;
    public static int MaxPlayerCount(ResolvedGridSpecs grid) => grid.Width / MinimumPlayerWidth;

    private static Color PlayerAreaColor(int player) => PlayerColors[player].Darkened(0.8f);
    private static Color PlayerColumnPressedColor(int player) => PlayerColors[player].Darkened(0.7f);
    public bool IsXCoordWithinSafeZone(int rx) => rx != _playerAreaWidth - 1 && rx != 0;

    protected override IReadOnlyList<Sounds> UsedSounds => [
        Sounds.Countdown_CountdownSoundEffect8Bit,
        Sounds.Flourish_8BitBlastK,
        Sounds.Flourish_90sGameUi4,
        Sounds.Flourish_CuteLevelUp3,
        Sounds.GameStart_8BitBlastE,
        Sounds.Music_Competitive_SparkGrooveElectroSwingDancyFunny,
        Sounds.Negative_ClassicGameActionNegative18,
        Sounds.Negative_RetroHurt1,
        Sounds.Negative_RetroHurt2,
        Sounds.Neutral_Button,
        Sounds.Positive_90sGameUi2,
        Sounds.GameWin_GoodResult,
    ];

    public GridRhythmGame(ResolvedGridSpecs grid, int playerCount, int randomSeed) : base(grid)
    {
        GDPrint.LogState(playerCount, randomSeed);
        int maxPlayerCount = MaxPlayerCount(grid);
        Assert.Clamp(ref playerCount, 1, maxPlayerCount);

        _playerCount = playerCount;
        _randomSeed = randomSeed;
        _rand = new Random(_randomSeed);
        _gameTimeElapsed = 0;

        InsideScreen!.Message.Show();

        _playerPlatformSelectionAreas = PlayerAreas.SplitHorizontally(GridSpecs.Width, PlatformHeight, playerCount, MaximumPlayerWidth);
        _playerTargetAreas = PlayerAreas.SplitHorizontally(GridSpecs.Width, GridSpecs.Height, playerCount, MaximumPlayerWidth);

        var playerWidth = _playerPlatformSelectionAreas.PlayerAreaSize.Width;
        var playerHeight = _playerPlatformSelectionAreas.PlayerAreaSize.Height;

        _playerAreaWidth = playerWidth;
        _playerColumns = new Polygon2D[_playerCount, playerWidth];
        _playerPlatforms = new Polygon2D[_playerCount];
        _playerTargets = new Queue<Polygon2D>[_playerCount, _playerAreaWidth];
        _playerPlatformSegments = new Polygon2D[_playerCount, _playerAreaWidth];
        _playerDespawnIndicators = new Polygon2D[_playerCount, _playerAreaWidth];
        _playerAreas = new Polygon2D[_playerCount];
        _playerScores = new int[_playerCount];

        for (var pi = 0; pi < _playerCount; pi++)
        {
            // Platforms
            var playerPlatformPolygon = MakePolygon(new(), _playerPlatformSelectionAreas.PlayerAreaSize.ToVector2(), PlayerColors[pi]);
            playerPlatformPolygon.Position = _playerPlatformSelectionAreas.Get(pi).ToVector2();
            _playerPlatformSelectionNode.AddChild(playerPlatformPolygon);
            _playerPlatforms[pi] = playerPlatformPolygon;

            // Areas
            var playerAreaPolygon = MakePolygon(new(0, 0), _playerTargetAreas.PlayerAreaSize.ToVector2(), PlayerAreaColor(pi));
            playerAreaPolygon.Position = _playerTargetAreas.Get(pi).ToVector2();
            _playerTargetAreaNode.AddChild(playerAreaPolygon);
            _playerAreas[pi] = playerAreaPolygon;

            // Individual Column within Areas, and platform segments
            for (var x = 0; x < _playerAreaWidth; x++)
            {
                _playerTargets[pi, x] = new Queue<Polygon2D>();

                var playerColumnPolygon = MakePolygon(new (0, PlatformHeight), new (1, GridSpecs.Height), Colors.Transparent);
                var columnPoint = _playerTargetAreas.Get(pi);
                columnPoint.X += x;
                playerColumnPolygon.Position = columnPoint.ToVector2();
                _playerColumnNode.AddChild(playerColumnPolygon);
                _playerColumns[pi, x] = playerColumnPolygon;

                var playerPlatformSegment = MakePolygon(new (0, 0), new (1, IsXCoordWithinSafeZone(x) ? 1 : 3), Colors.Transparent);
                var segmentPoint = _playerTargetAreas.Get(pi);
                segmentPoint.X += x;
                if(IsXCoordWithinSafeZone(x)) segmentPoint.Y+=2;
                playerPlatformSegment.Position = segmentPoint.ToVector2();
                _playerPlatformSegmentNode.AddChild(playerPlatformSegment);
                _playerPlatformSegments[pi, x] = playerPlatformSegment;

                var playerDespawnIndicator = MakePolygon(new(0, 0), new(1, 1), Colors.Transparent);
                var indicatorPoint = _playerTargetAreas.Get(pi);
                indicatorPoint.X += x;
                playerDespawnIndicator.Position = indicatorPoint.ToVector2();
                _playerDespawnIndicatorNode.AddChild(playerDespawnIndicator);
                _playerDespawnIndicators[pi, x] = playerDespawnIndicator;
            }

        }

        for (int i = 0; i < _playerCount; ++i)
        {
            _playerFlourishLayer.AddChild(new Flourish(new(playerWidth, playerHeight), new(playerWidth, playerHeight))
            {
                Position = _playerPlatformSelectionAreas.Get(i).ToVector2(),
                Visible = false,
            });
        }

        // Add in render order.
        AddChild(_playerTargetAreaNode);
        AddChild(_playerDespawnIndicatorNode);
        AddChild(_playerPlatformSegmentNode);
        AddChild(_playerPlatformSelectionNode);
        AddChild(_playerColumnNode);
        AddChild(_playerRankIndicatorNode);
        AddChild(_playerTargetNode);
        AddChild(Flourish);
        AddChild(_playerFlourishLayer);

        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _state = States.Flourish;
        _playerTargetAreaNode.Hide();
        _playerDespawnIndicatorNode.Hide();
        _playerPlatformSegmentNode.Hide();
        _playerPlatformSelectionNode.Hide();
        _playerColumnNode.Hide();
        _playerRankIndicatorNode.Hide();
        EnterFlourishState(EnterSelectColor);
    }

    private void EnterSelectColor()
    {
        _state = States.SelectColor;
        InsideScreen!.Message.Text = "Choose a colour";
        _playerTargetAreaNode.Show();
        _playerDespawnIndicatorNode.Show();
        _playerPlatformSegmentNode.Show();
        _playerPlatformSelectionNode.Show();
        _playerColumnNode.Show();

        var revealTime = SkipIntro ? 0.1f : 1f;
        for (int pi = 0; pi < _playerCount; pi++)
        {
            var delay = (pi + 1) * revealTime / _playerCount;
            var playerAreaPolygon = _playerAreas[pi];
            var playerPlatformPolygon = _playerPlatforms[pi];

            FlashColor(playerAreaPolygon, new(), PlayerAreaColor(pi), playerAreaPolygon.Color, delay);
            FlashColor(playerPlatformPolygon, new(), PlayerColors[pi], playerPlatformPolygon.Color, delay);
            this.AddTimer(delay).Timeout += () => AudioManager?.Play(Sounds.Positive_90sGameUi2);
        }
    }

    private void ProcessSelectColor()
    {
        var someSelected = false;
        foreach (var (x, y) in TakePressed())
        {
            var (pi, relX, _) = _playerPlatformSelectionAreas.GetPlayerRelative(x, y);
            if (pi == -1) { continue; }

            // Hide platform once selected
            var platform = (Polygon2D?)_playerPlatforms.GetValue(pi);
            if (platform == null) continue;
            if (!platform.Visible) continue;
            var tween = FadeOut(platform, Colors.Transparent);
            tween.Finished += platform.Hide;
            someSelected = true;

            for(var rx = 0; rx < _playerAreaWidth; rx++)
            {
                var segment = _playerPlatformSegments[pi, rx];
                FadeOut(segment, Colors.White);
            }
        }

        var allSelected = _playerPlatforms.All(p => !p.Visible);
        if (allSelected || SkipIntro)
        {
            for (var pi = 0; pi < _playerCount; pi++)
            {
                var platform = _playerPlatforms[pi];
                if (!platform.Visible) continue;
                var tween = FadeOut(platform, Colors.Transparent);
                tween.Finished += platform.Hide;
                someSelected = true;

                for(var rx = 0; rx < _playerAreaWidth; rx++)
                {
                    var segment = _playerPlatformSegments[pi, rx];
                    FadeOut(segment, Colors.White);
                }
            }
            EnterCountdown();
        }

        if (!someSelected) return;
        AudioManager?.Play(allSelected ? Sounds.GameStart_8BitBlastE : Sounds.Flourish_CuteLevelUp3);
    }

    private void EnterCountdown()
    {
        _state = States.Countdown;

        // Scale the time between countdown steps.
        var countdownIntervalSeconds = SkipIntro ? 0.1f : 1f;
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
            EnterPlay();
        };
    }

    private void EnterPlay()
    {
        _state = States.Play;
        var mp = AudioManager?.PlayMusic(Sounds.Music_Competitive_SparkGrooveElectroSwingDancyFunny);
        if(mp != null) mp.Finished += EnterGameOver;
        InitiatePeriodicMovement();
        InitiatePeriodicSpawns();

        // Clear any existing input.
        TakePressed();
    }


    private void ProcessPlay(double delta)
    {
        // Check for presses since last time.
        foreach (var (x, y) in TakePressed())
        {
            var (pi, rx, ry) = _playerPlatformSelectionAreas.GetPlayerRelative(x, y);
            if (pi == -1) { continue; }
            // Since we do not use player area for each individual segment, use the platform to determine player.
            // Only activate if step corresponds to the activation segments (y = 1)
            if (IsXCoordWithinSafeZone(x))
                if(ry == 0 || ry == 1) continue;

            var column = _playerColumns[pi, rx];

            // Provide debounce to avoid unintentional multi-step activations
            var timer = column.GetChildByType<Timer>();
            if (timer != null) continue;
            timer = new Timer();
            column.AddChild(timer);
            timer.Start(0.1);
            timer.Timeout += () => column.RemoveChild(timer);

            FlashColor(column,  PlayerAreaColor(pi), PlayerColumnPressedColor(pi), Colors.Transparent, initialTweenDuration: 0.1f, finalTweenDuration: 0.1f);

            var targetQueue = _playerTargets[pi, rx];
            var isEmpty = !targetQueue.TryPeek(out var targetToFree);
            if (isEmpty) continue;

            // Ignore if already being queued for deletion (i.e., despawning missed targets)
            if (targetToFree!.IsQueuedForDeletion()) continue;

            Color? colorToFlash = null;
            var scoreToGain = 0;

            // Determine the hit's timing precision
            var distance = targetToFree.Position.Y - PlatformHeight;
            switch (distance)
            {
                case < 1:
                    colorToFlash = Colors.Green;
                    scoreToGain = 3;
                    break;
                case < 3:
                    colorToFlash = Colors.Orange;
                    scoreToGain = 2;
                    break;
                case < 5:
                    colorToFlash = Colors.Red;
                    scoreToGain = 1;
                    break;
            }

            var segment = _playerPlatformSegments[pi, rx];

            // If color to flash is null, means the first in queue is too far from platform - still provide step feedback
            if (colorToFlash == null)
            {
                FlashColor(segment, Colors.White, Colors.Gray, Colors.White, initialTweenDuration: 0.25f, finalTweenDuration: 0.25f);
                continue;
            }

            if (colorToFlash.Value == Colors.Green)
            {
                // TODO - May need to adjust pitch or sound in the future to avoid monotonic repetitive sound feedback
                AudioManager?.Play(Sounds.Positive_90sGameUi2);
            }

            // Found a valid hit - despawn the target
            FlashColor(segment, Colors.White, colorToFlash.Value, Colors.White, initialTweenDuration: 0.25f, finalTweenDuration: 0.25f);
            targetQueue.Dequeue();
            var tween = FlashColor(targetToFree, targetToFree.Color, colorToFlash.Value, Colors.Transparent, initialTweenDuration: 0.1f, finalTweenDuration: 0.5f);
            tween.Finished += targetToFree.QueueFree;
            _playerScores[pi] += scoreToGain;
        }
        UpdateScoreMessage();
    }

    private void InitiatePeriodicSpawns()
    {
        var timerIntervalSeconds = 0.55;
        var timer = this.AddTimer(timerIntervalSeconds);
        timer.OneShot = false;
        
        timer.Timeout += () =>
        {
            // Stop the game stepping once state leaves Play
            if (_state != States.Play) return;
            SpawnTargets();
        };
    }

    private void InitiatePeriodicMovement()
    {
        var timerIntervalSeconds = 0.05;
        this.AddTimer(timerIntervalSeconds).Timeout += () =>
        {
            // Stop the game stepping once state leaves Play
            if (_state != States.Play) return;

            for (var pi = 0; pi < _playerCount; pi++)
            {
                for (var x = 0; x < _playerAreaWidth; x++)
                {
                    var queue = _playerTargets[pi, x];
                    var targets = queue.ToList();
                    foreach (var target in targets)
                    {
                        // Move towards the target one step at a time
                        var currentPos = target.Position;
                        var targetPos = new Vector2(currentPos.X, PlatformHeight - 3);

                        var yDiff = targetPos.Y - currentPos.Y;
                        if (currentPos == targetPos) target.Hide();
                        if((int)currentPos.X == (int)targetPos.X && yDiff > 3)
                        {
                            // If already about to be deleted, ignore
                            if (target.IsQueuedForDeletion()) continue;

                            // If target has reached the platform, despawn and indicate missed tile by flashing red
                            queue.Dequeue();
                            target.QueueFree();

                            var segment = _playerPlatformSegments[pi, x];
                            FlashColor(segment, Colors.White, Colors.Red, Colors.White);
                        }
                        else
                        {
                            target.Position += Grid2DMovementDirection.UP.ToVector2();
                        }
                    }
                }
            }
            InitiatePeriodicMovement();
        };
    }

    private void SpawnTargets()
    {
        var rx = _rand.Next(0, _playerAreaWidth);
        for (var pi = 0; pi < _playerCount; pi++)
        {
            var playerPoint = _playerTargetAreas.Get(pi);
            var playerColor = PlayerColors[pi];

            var polygon = MakePolygon(Vector2.Zero, new Vector2(1,1), Colors.Transparent);
            polygon.Position = new Vector2(playerPoint.X + rx, GridSpecs.Height);
            polygon.Color = playerColor;
            _playerTargetNode.AddChild(polygon);
            var queue = _playerTargets[pi, rx];
            queue.Enqueue(polygon);
        }
    }

    public override void _Process(double delta)
    {
        if (_state == States.Flourish) { ProcessFlourish(); }
        else if (_state == States.SelectColor) { ProcessSelectColor(); }
        else if (_state == States.Play)
        {
            _gameTimeElapsed += delta;
            ProcessPlay(delta);
        }
    }

    private void EnterGameOver()
    {
        _state = States.GameOver;
        AudioManager?.StopMusic();

        // TODO - make better animations to slowly cleanup before hiding
        // Clean up some nodes
        _playerPlatformSegmentNode.Hide();
        _playerTargetAreaNode.Hide();
        _playerTargetNode.Hide();
        _playerColumnNode.Hide();
        // Show the rank indicator node (no children yet)
        _playerRankIndicatorNode.Show();

        // Sort the scores by lowest to highest
        var sortedScores = new List<int>(_playerScores.ToList().OrderBy(s => s));
        var heightPerRank = GridSpecs.Height/_playerCount;
        for (var pi = 0; pi < _playerCount; pi++)
        {
            var playerScore = _playerScores[pi];

            // Rank is inverse - higher the number, the more points they earned
            var rank = sortedScores.IndexOf(playerScore) + 1;
            var height = rank * heightPerRank;

            // Create the polygons to indicate placings
            var rankPolygon = MakePolygon(new(0,0), new(_playerAreaWidth, height), Colors.Transparent);
            rankPolygon.Position = _playerTargetAreas.Get(pi).ToVector2();
            _playerRankIndicatorNode.AddChild(rankPolygon);

            // Visualize
            var persistPlayerIndex = pi;
            this.AddTimer(rank + 1).Timeout += () =>
            {
                FlashColor(rankPolygon, PlayerAreaColor(persistPlayerIndex), Colors.White, PlayerColors[persistPlayerIndex]);
                AudioManager?.Play(Sounds.Neutral_Button);

                if (rank == sortedScores.Count)
                {
                    this.AddTimer(1f).Timeout += () =>
                    {
                        FlashColorLoop(rankPolygon, rankPolygon.Color, Colors.Transparent, 1f, loopTotal: 5);
                        AudioManager?.Play(Sounds.GameWin_GoodResult);
                    };
                }
            };
        }
    }

    private void UpdateScoreMessage()
    {
        var playerScoreString = "";
        for (var pi = 0; pi < _playerCount; pi++)
        {
            playerScoreString += $"Player {pi + 1}: {_playerScores[pi]}\n";
        }

        InsideScreen!.Message.Text = playerScoreString;
    }
}
