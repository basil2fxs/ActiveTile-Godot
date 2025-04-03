using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Games.Collect;
using MysticClue.Chroma.GodotClient.GameLogic.Games.Memory;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Competitive grid game where players are required to memorize a pattern of tiles.
/// Players are then required to step on the same tiles, in any order.
/// If a player steps on a tile that isn't one of the revealed targets, their progress is reset.
/// </summary>
public partial class GridMemoryGame : GridGame
{
    private sealed record PlayerPolygon(Polygon2D TopPlatform, Polygon2D BottomPlatform, Polygon2D PlayerArea);

    private readonly Random _rand;

    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        var maxPlayers = int.Min(maxSupportedPlayers, MaxPlayerCount(gridSpecs));
        for (int playerCount = 2; playerCount <= maxPlayers; playerCount++)
        {
            foreach (var difficulty in Enum.GetValues<GameSelection.GameDifficulty>())
            {
                var config = difficulty switch {
                    GameSelection.GameDifficulty.Newbie => GridMemoryConfig.GetNewbiePvpConfig(),
                    GameSelection.GameDifficulty.Regular => GridMemoryConfig.GetRegularPvpConfig(),
                    GameSelection.GameDifficulty.Elite => GridMemoryConfig.GetElitePvpConfig(),
                    _ => throw new InvalidOperationException($"Invalid value for {typeof(GameSelection.GameDifficulty)}: {difficulty}"),
                };

                var playerCountCapture = playerCount;
                yield return new(
                    playerCount,
                    GameSelection.GameType.Competitive,
                    difficulty,
                    "Memory",
                    null,
                    "Remember the target path before they disappear, then traverse only along this path. Your progress will reset if you step on a wrong tile. 10 rounds.",
                    GD.Load<Texture2D>("res://HowToPlay/Memory.png"),
                    randomSeed => new GridMemoryGame(gridSpecs, playerCountCapture, randomSeed, config));
            }
        }

        // TODO - Support coop later
        // yield return new(
        //     maxSupportedPlayers,
        //     GameSelection.GameType.Cooperative,
        //     GameSelection.GameDifficulty.Regular,
        //     "CollectCoop",
        //     null,
        //     "How fast can you collect all the bright tiles?",
        //     GD.Load<Texture2D>("res://HowToPlay/CollectCoop.png"),
        //     randomSeed => new GridMemoryGame(gridSpecs, 1, randomSeed));
    }

    private enum GameStates
    {
        Flourish,
        SelectColor,
        Countdown,
        Play,
        GameOver,
    }
    private enum PlayerStates
    {
        Standby,
        ColorSelect,
        Ready,
        RoundStart,
        RevealTargets,
        RoundPlay,
        RoundEnd,
        Finished,
    }

    protected override IReadOnlyList<Sounds> UsedSounds => [
        Sounds.Music_Competitive_CruisingDown8BitLane,
        Sounds.Countdown_CountdownSoundEffect8Bit,
        Sounds.Flourish_8BitBlastK,
        Sounds.Flourish_90sGameUi4,
        Sounds.Flourish_CuteLevelUp3,
        Sounds.GameLose_GameOverArcade,
        Sounds.GameStart_8BitArcadeVdeoGameStartSoundEffectGunReloadAndJump,
        Sounds.GameStart_8BitBlastE,
        Sounds.GameWin_GoodResult,
        Sounds.GameWin_Yipee,
        Sounds.Negative_8BitGame1,
        Sounds.Negative_ClassicGameActionNegative18,
        Sounds.Neutral_Button,
        Sounds.Positive_90sGameUi2,
        Sounds.Positive_ClassicGameActionPositive30,
        Sounds.Positive_GameBonus,
    ];

    // Layers for animations.
    Node2D _playerFlourishLayer = new() { Name = "PlayerFlourishLayer" };

    // Containers/layers for game objects.
    Node2D _playerPlatformNode = new() { Name = "PlayerPlatformNode" };
    Node2D _playerTargetAreaNode = new() { Name = "PlayerTargetAreaNode" };
    Node2D _playerRankIndicatorNode = new() { Name = "PlayerRankIndicatorNode" };
    Node2D _playerTargetNode = new() { Name = "PlayerTargetNode" };

    // Player separated areas
    PlayerAreas _playerTargetAreas;
    PlayerAreas _playerPlatformAreas;

    // Player tracking collections
    private Flourish[] _playerFlourishes;
    private PlayerPolygon[] _playerPolygons;
    private Tween[] _playerPlatformPrompterTweens;
    private Dictionary<Polygon2D, bool>[] _playerActiveTargets;
    private Polygon2D[,] _playerPossibleTargets;
    private int[] _perRoundSeeds;
    private int[] _playerCurrentRound;
    private int[] _playerFailedAttempts;
    private int[] _playerScores;
    private List<int> _playersFinished = [];

    // Game properties
    int _playerCount;
    int _randomSeed;
    const int MinimumPlayerWidth = 3;
    private double _gameTimeElapsed;
    private const int PlatformHeight = 2;
    private int PlayerAreaWidth => _playerTargetAreas.PlayerAreaSize.Width;
    private int PlayerAreaHeight => _playerTargetAreas.PlayerAreaSize.Height;
    // private int PlatformTopIndex => 0;
    // private int PlatformBottomIndex => 1;
    private int TotalRounds => _config.TotalRounds;
    private GameStates _gameState;
    private PlayerStates[] _playerStates;

    // Game Colours
    private static Color PlayerAreaColor(int player) => PlayerColors[player].Darkened(0.8f);
    private static Color IncorrectTileColor => Colors.Black;
    private static Color PlatformColor => Colors.White;
    private static Color RoundReadyColor => Colors.Green;
    private static Color RoundRevealingTargetsColor => Colors.Red;

    public static int MaxPlayerCount(ResolvedGridSpecs grid) => grid.Width / MinimumPlayerWidth;
    public static float FlourishSeconds => 1.5f;

    private GridMemoryConfig.GameConfig _config;

    public GridMemoryGame(ResolvedGridSpecs grid, int playerCount, int randomSeed, GridMemoryConfig.GameConfig config) : base(grid)
    {
        GDPrint.LogState(config, playerCount, randomSeed);
        _randomSeed = randomSeed;
        _rand = new Random(_randomSeed);
        _config = config;

        int maxPlayerCount = MaxPlayerCount(grid);
        Assert.Clamp(ref playerCount, 1, maxPlayerCount);

        InsideScreen!.Message.Show();

        _playerCount = playerCount;
        _randomSeed = randomSeed;

        _playerPlatformAreas = PlayerAreas.SplitHorizontally(GridSpecs.Width, PlatformHeight, playerCount);
        _playerTargetAreas = PlayerAreas.SplitHorizontally(GridSpecs.Width, GridSpecs.Height, playerCount);

        // Initialize collection trackers
        _playerPolygons = new PlayerPolygon[_playerCount];
        _playerActiveTargets = new Dictionary<Polygon2D, bool>[_playerCount];
        _playerStates = new PlayerStates[_playerCount];
        _playerCurrentRound = new int[_playerCount];
        _playerFailedAttempts = new int[_playerCount];
        _playerPossibleTargets = new Polygon2D[GridSpecs.Width, GridSpecs.Height];
        _playerFlourishes = new Flourish[_playerCount];
        _playerPlatformPrompterTweens = new Tween[_playerCount];
        _playerScores = new int[_playerCount];

        for (var pi = 0; pi < _playerCount; pi++)
        {
            _playerActiveTargets[pi] = new Dictionary<Polygon2D, bool>();
            _playerCurrentRound[pi] = 1;
            _playerFailedAttempts[pi] = 0;

            // Platforms
            var playerPlatformPosTop = _playerPlatformAreas.Get(pi).ToVector2();
            var playerPlatformPolygonTop = MakePolygon(new(), _playerPlatformAreas.PlayerAreaSize.ToVector2(), PlayerColors[pi]);
            playerPlatformPolygonTop.Position = playerPlatformPosTop;
            _playerPlatformNode.AddChild(playerPlatformPolygonTop);

            var playerPlatformPosBottom = _playerPlatformAreas.Get(pi).ToVector2();
            playerPlatformPosBottom.Y += PlayerAreaHeight - PlatformHeight;
            var playerPlatformPolygonBottom = MakePolygon(new(), _playerPlatformAreas.PlayerAreaSize.ToVector2(), Colors.Transparent);
            playerPlatformPolygonBottom.Position = playerPlatformPosBottom;
            _playerPlatformNode.AddChild(playerPlatformPolygonBottom);

            // Areas
            var playerAreaPolygon = MakePolygon(new(0, 0), _playerTargetAreas.PlayerAreaSize.ToVector2(), PlayerAreaColor(pi));
            playerAreaPolygon.Position = _playerTargetAreas.Get(pi).ToVector2();
            _playerTargetAreaNode.AddChild(playerAreaPolygon);
            _playerStates[pi] = PlayerStates.Standby;

            // Build player polygon record
            _playerPolygons[pi] = new PlayerPolygon(playerPlatformPolygonTop, playerPlatformPolygonBottom, playerAreaPolygon);

            var anchorPoint = _playerTargetAreas.Get(pi);
            for (var x = anchorPoint.X; x < anchorPoint.X + PlayerAreaWidth; x++)
            {
                for (var y = anchorPoint.Y; y < GridSpecs.Height; y++)
                {
                    var polygon = MakePolygon(Vector2.Zero, new Vector2(1,1), Colors.Transparent);
                    polygon.Position = new Vector2(x, y);
                    _playerTargetNode.AddChild(polygon);
                    _playerPossibleTargets[x, y] = polygon;
                }
            }
        }

        for (int pi = 0; pi < _playerCount; ++pi)
        {
            var playerFlourish =
                new Flourish(new(PlayerAreaWidth, PlayerAreaHeight), new(PlayerAreaWidth, PlayerAreaHeight))
                {
                    Position = _playerPlatformAreas.Get(pi).ToVector2(),
                    Visible = false,
                };
            _playerFlourishLayer.AddChild(playerFlourish);
            _playerFlourishes[pi] = playerFlourish;
        }

        _perRoundSeeds = new int[TotalRounds];
        for (var i = 0; i < TotalRounds; i++)
        {
            _perRoundSeeds[i] = _rand.Next();
        }

        // Add in render order.
        AddChild(_playerTargetAreaNode);
        AddChild(_playerPlatformNode);
        AddChild(_playerFlourishLayer);
        AddChild(_playerTargetNode);
        AddChild(_playerRankIndicatorNode);
        AddChild(Flourish);

        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _gameState = GameStates.Flourish;
        _playerTargetAreaNode.Hide();
        _playerPlatformNode.Hide();
        _playerRankIndicatorNode.Hide();
        EnterFlourishState(EnterSelectColor);
    }

    private void EnterSelectColor()
    {
        _gameState = GameStates.SelectColor;
        InsideScreen!.Message.Text = "Choose a colour";
        _playerTargetAreaNode.Show();
        _playerPlatformNode.Show();

        var revealTime = SkipIntro ? 0.1f : 1f;
        for (int pi = 0; pi < _playerCount; pi++)
        {
            var delay = (pi + 1) * revealTime / _playerCount;
            var playerAreaPolygon = _playerPolygons[pi].PlayerArea;
            var playerTopPlatformPolygon = _playerPolygons[pi].TopPlatform;
            var playerBottomPlatformPolygon = _playerPolygons[pi].BottomPlatform;

            FlashColor(playerAreaPolygon, new(), PlayerAreaColor(pi), playerAreaPolygon.Color, delay);
            FlashColor(playerTopPlatformPolygon, new(), PlayerColors[pi], playerTopPlatformPolygon.Color, delay);
            this.AddTimer(delay).Timeout += () => AudioManager?.Play(Sounds.Positive_90sGameUi2);
            _playerStates[pi] = PlayerStates.ColorSelect;
        }
    }
    private void ProcessSelectColor()
    {
        foreach (var (x, y) in TakePressed())
        {
            var (pi, relX, _) = _playerPlatformAreas.GetPlayerRelative(x, y);
            if (pi == -1) { continue; }

            if (_playerStates[pi] == PlayerStates.Ready) continue;
            _playerStates[pi] = PlayerStates.Ready;
            // Hide platform once selected
            var topPlatform = ((PlayerPolygon?)_playerPolygons.GetValue(pi))?.TopPlatform;
            if (topPlatform == null) continue;
            if (!topPlatform.Visible) continue;
            FadeOut(topPlatform, PlatformColor);

            AudioManager?.Play(Sounds.Flourish_CuteLevelUp3);
        }

        var allSelected = _playerStates.All(s => s == PlayerStates.Ready);
        if (!allSelected && !SkipIntro)
            return;

        for (var pi = 0; pi < _playerCount; pi++)
        {
            var topPlatform = ((PlayerPolygon?)_playerPolygons.GetValue(pi))?.TopPlatform;
            if (topPlatform == null) continue;
            if (!topPlatform.Visible) continue;
            FadeOut(topPlatform, PlatformColor);

            var bottomPlatform = ((PlayerPolygon?)_playerPolygons.GetValue(pi))?.BottomPlatform;
            if (bottomPlatform == null) continue;
            if (!bottomPlatform.Visible) continue;
            FadeOut(bottomPlatform, Colors.Transparent);
        }

        AudioManager?.Play(Sounds.GameStart_8BitBlastE);
        EnterCountdown();
    }

    private void EnterCountdown()
    {
        _gameState = GameStates.Countdown;
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
        _gameState = GameStates.Play;
        InitiateBackgroundMusic();

        for (var pi = 0; pi < _playerCount; pi++)
        {
            EnterRoundStart(pi);
        }
        // Clear any existing input.
        TakePressed();
    }

    private void InitiateBackgroundMusic()
    {
        AudioManager?.PlayMusic(Sounds.Music_Competitive_CruisingDown8BitLane);
    }

    private void EnterRoundStart(int playerIndex)
    {
        _playerStates[playerIndex] = PlayerStates.RoundStart;
        var topPlatform = _playerPolygons[playerIndex].TopPlatform;
        var bottomPlatform = _playerPolygons[playerIndex].BottomPlatform;

        var currentRound = _playerCurrentRound[playerIndex];
        var startingPlatform = currentRound % 2 != 0 ? topPlatform : bottomPlatform;
        var endingPlatform = currentRound % 2 == 0 ? topPlatform : bottomPlatform;

        var tween = FadeOut(startingPlatform, PlatformColor);
        FadeOut(endingPlatform, Colors.Transparent);

        if (_playerCurrentRound[playerIndex] == 1 && _playerFailedAttempts[playerIndex] == 0)
        {
            ExitRoundStart(playerIndex, 0);
            return;
        }

        tween.Finished += () =>
        {
            _playerStates[playerIndex] = PlayerStates.RoundStart;
            InitiatePlatformFlashingLoop(playerIndex, startingPlatform);
        };
    }

    private void InitiatePlatformFlashingLoop(int playerIndex, Polygon2D polygon)
    {
        polygon.Show();
        var tween = FlashColorLoop(polygon, PlatformColor, Colors.Transparent, 1f);
        _playerPlatformPrompterTweens[playerIndex] = tween;
        tween.Finished += () =>
        {
            var playerState = _playerStates[playerIndex];
            if (playerState != PlayerStates.RoundStart)
            {
                return;
            }
            InitiatePlatformFlashingLoop(playerIndex, polygon);
        };
    }

    private void ExitRoundStart(int playerIndex, int ry)
    {
        var topPlatform = _playerPolygons[playerIndex].TopPlatform;
        var bottomPlatform = _playerPolygons[playerIndex].BottomPlatform;

        var currentRound = _playerCurrentRound[playerIndex];
        Polygon2D startingPlatform;

        // Only assign the starting platform if the pressed platform corresponds to the correct side
        if (0 <= ry && ry < PlatformHeight && currentRound % 2 != 0)
        {
            startingPlatform = topPlatform;
        }
        else if (PlayerAreaHeight - PlatformHeight <= ry && ry < PlayerAreaHeight && currentRound % 2 == 0)
        {
            startingPlatform = bottomPlatform;
        }
        else
        {
            return;
        }

        var existingPrompterTween = (Tween?)_playerPlatformPrompterTweens.GetValue(playerIndex);
        if (existingPrompterTween != null && existingPrompterTween.IsRunning())
        {
            existingPrompterTween.Stop();
        }
        var tween = FadeOut(startingPlatform, RoundRevealingTargetsColor);
        tween.Finished += () => { startingPlatform.Color = RoundRevealingTargetsColor; };
        EnterRevealTargets(playerIndex);
    }

    private void EnterRevealTargets(int playerIndex)
    {
        _playerStates[playerIndex] = PlayerStates.RevealTargets;
        AudioManager?.Play(Sounds.Positive_GameBonus);
        var playerPoint = _playerTargetAreas.Get(playerIndex);
        var playerColor = PlayerColors[playerIndex];

        int? prevRx = null;
        var playerMinRx = 0;
        var playerMaxRx = PlayerAreaWidth - 1;

        var currentRound = _playerCurrentRound[playerIndex];
        if (currentRound > TotalRounds) return;
        var roundRand = new Random(_perRoundSeeds[currentRound-1]);
        var maxY = GridSpecs.Height - 1;

        var startingFromTopPlatform = _playerCurrentRound[playerIndex] % 2 != 0;
        var step = startingFromTopPlatform ? 1 : -1;
        var i = 0;
        var singleTargetDelay = _config.TargetRevealSpawnIntervalSeconds;
        if (_config.FasterRevealPerRound)
            singleTargetDelay -= _playerCurrentRound[playerIndex] * _config.FasterRevealDurationDecreaseSeconds;

        for (var y = startingFromTopPlatform ? PlatformHeight : maxY - PlatformHeight; startingFromTopPlatform ? y <= maxY : y >= 0;  y+=step)
        {
            var minRx = playerMinRx;
            var maxRx = playerMaxRx;
            if (prevRx != null)
            {
                minRx = prevRx <= playerMinRx ? prevRx.Value : prevRx.Value - 1;
                maxRx = prevRx >= playerMaxRx ? prevRx.Value : prevRx.Value + 1;
            }

            var rx = roundRand.Next(minRx, maxRx + 1);
            var globX = playerPoint.X + rx;

            var polygon = _playerPossibleTargets[globX, y];
            var targets = _playerActiveTargets[playerIndex];
            targets.TryAdd(polygon, false);

            var tween = FlashColor(polygon, Colors.Transparent, playerColor, playerColor,i*singleTargetDelay);
            if (_config.StaggeredReveal)
            {
                tween.Finished += () => { FadeOut(polygon, Colors.Transparent); };
            }
            prevRx = rx;
            i++;
        }

        // Hide all the targets simultaneously
        var totalSpawnDuration = maxY * singleTargetDelay;
        var totalRemainDuration = totalSpawnDuration + _config.TargetRevealRemainDurationSeconds;
        if (_config.FasterRevealPerRound)
            totalRemainDuration -= _playerCurrentRound[playerIndex] * _config.FasterRevealDurationDecreaseSeconds;
        this.AddTimer(totalRemainDuration).Timeout += () =>
        {
            if (!_config.StaggeredReveal)
            {
                foreach (var (polygon, _) in _playerActiveTargets[playerIndex])
                {
                    FlashColor(polygon, playerColor, Colors.White, Colors.Transparent);
                }
            }

            var playerPolygon = _playerPolygons[playerIndex];
            var startingPlatform = startingFromTopPlatform ? playerPolygon.TopPlatform : playerPolygon.BottomPlatform;
            EnterRoundPlay(playerIndex, startingPlatform);
        };
    }

    private void EnterRoundPlay(int playerIndex, Polygon2D platform)
    {
        _playerStates[playerIndex] = PlayerStates.RoundPlay;
        FadeOut(platform, RoundReadyColor);
    }

    private void ProcessRoundPlay(int pi, int rx, int ry)
    {
        var playerPoint = _playerTargetAreas.Get(pi);
        var playerColor = PlayerColors[pi];
        var globX = playerPoint.X + rx;

        var pressedTarget = (Polygon2D?)_playerPossibleTargets.GetValue(globX, ry);
        if (pressedTarget == null)
            return;

        var playerCurrentRound = _playerCurrentRound[pi];
        // Ignore out of bounds
        if (playerCurrentRound % 2 != 0 && ry < PlatformHeight)
            return;
        if (playerCurrentRound % 2 == 0 && ry >= PlayerAreaHeight - PlatformHeight)
            return;
        // Incorrectly guessed already - ignore
        if (pressedTarget.Color == IncorrectTileColor) return;
        if (_playerActiveTargets[pi].TryGetValue(pressedTarget, out var isPressed) && isPressed) return;
        // Player guessed correctly - safe zone
        if (pressedTarget.Color == playerColor) return;

        if (_playerActiveTargets[pi].ContainsKey(pressedTarget))
        {
            // Correct Guess
            FlashColor(pressedTarget, pressedTarget.Color, Colors.White, playerColor);
            AudioManager?.Play(Sounds.Positive_90sGameUi2);
            _playerActiveTargets[pi][pressedTarget] = true;
            var allPressed = _playerActiveTargets[pi].Values.All(t => t);
            if (!allPressed)
                return;

            AudioManager?.Play(Sounds.Positive_ClassicGameActionPositive30);

            // Visual effect to show all pressed tiles
            var i = 0;
            foreach (var target in _playerActiveTargets[pi].Keys.Reverse())
            {
                FlashColor(target, pressedTarget.Color, Colors.White, Colors.Transparent, i * 0.1);
                i++;
            }

            // Flourish to cleanup the target area, then end the round
            this.AddTimer(1f).Timeout += () =>
            {
                var playerFlourish = _playerFlourishes[pi];
                playerFlourish.Show();
                playerFlourish.StartFromIndex(new Vector2(playerPoint.X, playerPoint.Y), FlourishSeconds);
                AudioManager?.Play(Sounds.GameStart_8BitArcadeVdeoGameStartSoundEffectGunReloadAndJump);
                this.AddTimer(FlourishSeconds).Timeout += playerFlourish.Hide;
                EnterRoundEnd(pi);
            };
        }
        else
        {
            // Incorrect Guess
            FlashColor(pressedTarget, pressedTarget.Color, Colors.White, IncorrectTileColor);
            AudioManager?.Play(Sounds.Negative_ClassicGameActionNegative18);
            this.AddTimer(1f).Timeout += () =>
            {
                AudioManager?.Play(Sounds.GameLose_GameOverArcade);
                ResetActiveTargets(pi);
                _playerActiveTargets[pi].Clear();
                EnterRoundStart(pi);
            };
            _playerFailedAttempts[pi]++;
            _playerStates[pi] = PlayerStates.Standby;
        }
    }

    private void ResetActiveTargets(int playerIndex)
    {
        var i = 0;
        foreach (var target in _playerActiveTargets[playerIndex].Keys.Reverse())
        {
            if (target.Color != PlayerColors[playerIndex]) continue;
            FlashColor(target, target.Color, Colors.White, Colors.Transparent, i*0.25);
            i++;
        }
    }

    private void ResetPossibleTargets(int playerIndex)
    {
        var playerPoint = _playerTargetAreas.Get(playerIndex);
        for (var x = playerPoint.X; x < playerPoint.X + PlayerAreaWidth; x++)
        {
            for (var y = playerPoint.Y; y < playerPoint.Y + PlayerAreaHeight; y++)
            {
                var target = _playerPossibleTargets[x, y];
                if (target.Color == PlayerAreaColor(playerIndex)) continue;
                FlashColor(target, target.Color, Colors.White, Colors.Transparent);
            }
        }
    }

    private void EnterRoundEnd(int playerIndex)
    {
        _playerStates[playerIndex] = PlayerStates.RoundEnd;
        this.AddTimer(1f).Timeout += () =>
        {
            // Clear all tiles in the target area - both targets and incorrect guesses
            ResetPossibleTargets(playerIndex);
            _playerActiveTargets[playerIndex].Clear();

            _playerCurrentRound[playerIndex]++;
            if (_playerCurrentRound[playerIndex] <= TotalRounds)
            {
                // If total rounds has not been completed, then go back to round start
                EnterRoundStart(playerIndex);
            }
            else
            {
                // Otherwise, end the game for this player
                EnterPlayerFinished(playerIndex);
            }
        };
    }

    private void EnterPlayerFinished(int playerIndex)
    {
        _playerStates[playerIndex] = PlayerStates.Finished;
        AudioManager?.Play(Sounds.GameWin_Yipee);

        var topPlatform = _playerPolygons[playerIndex].TopPlatform;
        var bottomPlatform = _playerPolygons[playerIndex].BottomPlatform;
        FadeOut(topPlatform, Colors.Transparent).Finished += topPlatform.Hide;
        FadeOut(bottomPlatform, Colors.Transparent).Finished += bottomPlatform.Hide;

        // Clear player specific polygons
        var area = _playerPolygons[playerIndex].PlayerArea;
        FadeOut(area, Colors.Transparent).Finished += area.Hide;

        _playersFinished.Add(playerIndex);
    }

    private void EnterGameOver()
    {
        _gameState = GameStates.GameOver;
        AudioManager?.StopMusic();

        // Clean up some nodes
        _playerPlatformNode.Hide();
        _playerTargetAreaNode.Hide();
        _playerTargetNode.Hide();
        // Show the rank indicator node (no children yet)
        _playerRankIndicatorNode.Show();
        // Sort the scores by lowest to highest
        _playersFinished.Reverse();
        var heightPerRank = GridSpecs.Height/_playerCount;
        foreach(var (pi, rank) in _playersFinished.Select((player, rank) => (player, rank)))
        {
            // Rank is inverse - higher the number, the more points they earned
            var height = (rank + 1) * heightPerRank;

            // Create the polygons to indicate placings
            var rankPolygon = MakePolygon(new(0,0), new(PlayerAreaWidth, height), Colors.Transparent);
            rankPolygon.Position = _playerTargetAreas.Get(pi).ToVector2();
            _playerRankIndicatorNode.AddChild(rankPolygon);

            // Visualize
            var persistPlayerIndex = pi;
            this.AddTimer(rank + 1).Timeout += () =>
            {
                FlashColor(rankPolygon, PlayerAreaColor(persistPlayerIndex), Colors.White, PlayerColors[persistPlayerIndex]);
                AudioManager?.Play(Sounds.Neutral_Button);

                if (rank == _playersFinished.Count - 1)
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

    private void ProcessPlay(double delta)
    {
        if (_playerStates.All(s => s == PlayerStates.Finished))
        {
            EnterGameOver();
            return;
        }

        // Check for presses since last time.
        foreach (var (x, y) in TakePressed())
        {
            var (pi, rx, ry) = _playerTargetAreas.GetPlayerRelative(x, y);
            if (pi == -1) { continue; }

            switch (_playerStates[pi])
            {
                case PlayerStates.RoundStart:
                    ExitRoundStart(pi, ry);
                    break;
                case PlayerStates.RoundPlay:
                    ProcessRoundPlay(pi, rx, ry);
                    break;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_gameState == GameStates.Flourish) { ProcessFlourish(); }
        else if (_gameState == GameStates.SelectColor) { ProcessSelectColor(); }
        else if (_gameState == GameStates.Play)
        {
            _gameTimeElapsed += delta;
            ProcessPlay(delta);
        }
    }

}
