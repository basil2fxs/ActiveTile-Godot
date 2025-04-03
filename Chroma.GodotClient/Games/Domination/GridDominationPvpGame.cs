using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.Games.Domination.Config;
using Color = Godot.Color;
using GridHelper = MysticClue.Chroma.GodotClient.Games.Domination.GridDominationGridHelper;

namespace MysticClue.Chroma.GodotClient.Games.Domination;

// TODO - Get feedback on flashing colour. Thinking better way to indicate targets different brightness of the same hue
/// <summary>
/// PVP grid game where each opposing player aim to capture as many tiles as possible within the time limit.
/// In intervals, each player will have tiles flash their color indicating that they are targets.
/// These will relocate after a duration if not captured. If they are captured, the next wave of targets will spawn immediately.
/// Each tile a player steps on will capture surrounding tiles in a single tile radius, with a few exceptions.
/// If the surrounding tiles are not captured, it will simply be captured by that player.
/// If the surrounding tiles are marked as targets for another player, it will simply be captured by that player as a steal
/// If the surrounding tiles are already captured by another player, the captured state will be cleared.
/// </summary>
public partial class GridDominationPvpGame : GridGame
{
    private readonly Random _rand;
    private readonly float _playerColorDarkness = 0.5f;

    private readonly Color _uncapturedColor = Colors.Transparent;

    private readonly GridHelper _gridHelper;
    private readonly ResolvedGridSpecs _grid;
    private readonly GridDominationConfig.GamePvpConfig _gameConfig;

    private readonly Dictionary<Point, bool> _playerTileSelected = [];
    private readonly Dictionary<int, List<Polygon2D>> _playerTilesCaptured;
    private readonly List<Polygon2D>[] _playerTargetTiles;
    private readonly Dictionary<Polygon2D, Tween>[] _playerTargetTweens;

    private readonly Polygon2D?[,] _playingFieldPolygons;

    private bool _rushModeActivated;
    private readonly int _randomSeed;
    private readonly int _playerCount;

    private double _gameTimeElapsed;

    private Node2D _playingField = new() { Name = "FieldNodes" };
    private Node2D _topLayer = new() { Name = "TopLayer" };
    private Flourish _playerRevealFlourish;

    private enum States { Flourish, PreReveal, RevealPlayers, SelectColor, Countdown, Play, GameOver };
    private States _state = States.Flourish;
    public override string StateString => _state.ToString();

    private bool _isGameInitialized;

    protected override IReadOnlyList<Sounds> UsedSounds =>
    [
        Sounds.Flourish_90sGameUi4,
        Sounds.Music_Competitive_CreepyDevilDance,
        Sounds.Positive_ClassicGameActionPositive5,
        Sounds.GameWin_YouWinSequence1,
        Sounds.Flourish_90sGameUi4,
        Sounds.Flourish_CuteLevelUp2,
        Sounds.Countdown_CountdownSoundEffect8Bit,
        Sounds.Neutral_Button
    ];

    public static int MaxPlayerCount(ResolvedGridSpecs gridSpecs, GridDominationConfig.GamePvpConfig config)
    {
        // Calculate expected capture per pressed tile
        // Radius = 1, Total Tile Per Capture = 9 (3^2)
        // Radius = 2, Total Tile Per Capture = 25 (5^2)
        // Radius = 3, Total Tile Per Capture = 45 (7^2)
        var captureRadiusSide = (config.PlayerTargetTileConfig.CaptureRadius * 2) + 1;
        var totalTilesPerCapture = Math.Pow(captureRadiusSide, 2);
        // Per player, want players to have capture-able tiles equivalent to 2 captures (based on capture radius)
        var captureCountPerPlayer = 2;
        int totalTiles = gridSpecs.Width * gridSpecs.Height;
        return (int)(totalTiles / (captureCountPerPlayer * totalTilesPerCapture));
    }

    public GridDominationPvpGame(
        ResolvedGridSpecs grid,
        GridDominationConfig.GamePvpConfig gameConfig,
        GridHelper gridHelper,
        int playerCount,
        int randomSeed) : base(grid)
    {
        GDPrint.LogState(gameConfig, gridHelper, randomSeed);

        _gridHelper = gridHelper;
        _randomSeed = randomSeed;
        _rand = new Random(_randomSeed);
        _grid = grid;
        _playingFieldPolygons = new Polygon2D[_grid.Width, _grid.Height];
        _gameConfig = gameConfig;

        var maxPlayerCount = MaxPlayerCount(_grid, _gameConfig);
        if (playerCount > maxPlayerCount) Assert.Report(new ArgumentException($"Game player count is greater than max player count (max={maxPlayerCount})."));
        _playerCount = Math.Min(playerCount, maxPlayerCount);

        _playerTargetTiles = new List<Polygon2D>[_playerCount];
        _playerTargetTweens = new Dictionary<Polygon2D, Tween>[_playerCount];
        _playerTilesCaptured = [];

        // Initialize polygon and tweens tracking structures
        for (var pi = 0; pi < _playerCount; pi++)
        {
            _playerTargetTiles[pi] = [];
            _playerTargetTweens[pi] = [];
            _playerTilesCaptured[pi] = [];
        }

        // Initialize inside screen
        _rushModeActivated = false;
        InsideScreen!.TimeDisplay.Show();
        InsideScreen.Message.Show();

        // Initialize playing field nodes
        var fieldNodeSize = new Vector2(1, 1);
        for (var x = 0; x < _grid.Width; ++x)
        {
            for (var y = 0; y < _grid.Height; ++y)
            {
                var polygon = MakePolygon(Vector2.Zero, fieldNodeSize, Colors.Transparent);
                polygon.Position = new Vector2(x, y);
                polygon.Name = $"FieldNode_{x}_{y}";
                _playingField.AddChild(polygon);
                _playingFieldPolygons[x, y] = polygon;
            }
        }

        // Define flourish layers
        _playerRevealFlourish = new Flourish(new Vector2(_grid.Width, _grid.Height), new Vector2I(_grid.Width, _grid.Height));

        // Add node layers
        AddChild(_playerRevealFlourish);
        AddChild(_playingField);
        AddChild(Flourish);
        AddChild(_topLayer);

        _isGameInitialized = true;

        // Visualize start of game
        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _state = States.Flourish;
        _playingField.Hide();
        EnterFlourishState(EnterPreReveal);
    }

    private void EnterPreReveal()
    {
        _state = States.PreReveal;
        InsideScreen!.Message.Text = "Get Ready";
    }

    private void ProcessPreReveal()
    {
        // TODO - May have plans to do stuff in this stage
        EnterPlayerTilesReveal();
    }

    private void EnterPlayerTilesReveal()
    {
        _state = States.RevealPlayers;
        InsideScreen!.Message.Text = "Here are your starting tiles!";

        var capturedTilesRevealSeconds = SkipIntro ? 0.1f : 2f;
        this.AddTimer(capturedTilesRevealSeconds).Timeout += () =>
        {
            var durationSeconds = SkipIntro ? 0.1 : 2;
            // Avoid the outer layers of the grid so that all players have identical starting positions once colors are selected
            var positionsToAvoid = GridHelper.GetGridOuterLayerPositions(_grid, _gameConfig.PlayerTargetTileConfig.CaptureRadius).ToList();
            var revealIntervalSeconds = durationSeconds / _playerCount;
            var pi = 0;

            // Should be covered by the Assert in the constructor, but keeping an extra check to avoid infinite loops
            var gridTotalTiles = _grid.Width * _grid.Height;
            if (_playerCount > gridTotalTiles) Assert.Report(new InvalidOperationException("Player count exceeds the total grid size"));
            var finalPlayerCount = Math.Min(_playerCount, gridTotalTiles);
            while (pi < finalPlayerCount)
            {
                var randomPosition = _gridHelper.TryGetRandomPositionWithinGrid(_grid, positionsToAvoid);
                var polygons = _playingField.GetChildrenByType<Polygon2D>();
                var playerPolygon = polygons.FirstOrDefault(p => GridSpecs.ToGridPosition(p.GlobalPosition) == randomPosition);
                if (playerPolygon == null || randomPosition == null) continue;
                positionsToAvoid.Add(randomPosition.Value);

                // Get positions in radius of player tile
                var radiusPositions = GridHelper.GetPositionsByRadius(_grid, randomPosition.Value.X, randomPosition.Value.Y, _gameConfig.PlayerTargetTileConfig.CaptureRadius * 2);
                // Add radius positions for positions to avoid to make sure no one gets to steal any player's tiles at start
                positionsToAvoid.AddRange(radiusPositions);

                // Reveal
                var delay = pi * revealIntervalSeconds;
                var playerColor = PlayerColors[pi];
                playerPolygon.Color = playerColor;
                EaseInFlashWhite(playerPolygon, delay);
                _playerTileSelected.TryAdd(randomPosition.Value, false);
                _playerTilesCaptured[pi].Add(playerPolygon);
                pi++;
            }

            _playingField.Show();
            this.AddTimer(durationSeconds + 1).Timeout += EnterSelectColor;
        };
    }

    private void EnterSelectColor()
    {
        _state = States.SelectColor;
        InsideScreen!.Message.Text = "Choose your color";
    }

    private void ProcessSelectColor()
    {
        foreach (var (x, y) in TakePressed())
        {
            var pressedPosition = new Point(x, y);
            // If not part of tile selection, ignore
            if (!_playerTileSelected.TryGetValue(pressedPosition, out var isSelected)) continue;
            // If already selected, ignore
            if (isSelected) continue;

            _playerTileSelected[pressedPosition] = true;
            var pressedPolygon = _playingFieldPolygons[x, y];
            if (pressedPolygon == null) continue;

            var totalSelected = _playerTileSelected.Count(p => p.Value);
            var pitchScale = float.Lerp(0.7f, 1, (float)totalSelected / _playerTileSelected.Count);
            AudioManager?.Play(Sounds.Flourish_CuteLevelUp2, pitchScale);
            // Get positions in radius of pressed tile to perform initial capture
            var radiusPositions = GridHelper.GetPositionsByRadius(_grid, pressedPosition.X, pressedPosition.Y, 1);
            List<Polygon2D> radiusPolygons = [];
            foreach (var rp in radiusPositions)
            {
                var radiusPolygon = _playingFieldPolygons.GetValue(rp.X, rp.Y) as Polygon2D;
                if (radiusPolygon == null) continue;
                radiusPolygons.Add(radiusPolygon);
            }
            var playerColor = pressedPolygon.Color;
            var playerIndex = PlayerColors.ToList().IndexOf(playerColor);
            var darkenedPlayerColor = playerColor.Darkened(_playerColorDarkness);
            pressedPolygon.Color = darkenedPlayerColor;
            foreach (var radiusPolygon in radiusPolygons)
            {
                FlashColor(radiusPolygon, _uncapturedColor, playerColor, darkenedPlayerColor);
                radiusPolygon.Color = darkenedPlayerColor;

                if (!_playerTilesCaptured[playerIndex].Contains(radiusPolygon))
                    _playerTilesCaptured[playerIndex].Add(radiusPolygon);
            }

            // Perform flourish based on player colour
            const float flourishSeconds = 1.5f;
            _playerRevealFlourish.Show();
            _playerRevealFlourish.StartFromIndex(pressedPosition.ToVector2(), flourishSeconds, pressedPolygon.Color);

            // If all has been selected, start the game
            if (_playerTileSelected.All(kvp => kvp.Value))
            {
                this.AddTimer(2).Timeout += EnterCountdown;
            }
        }
    }

    private void EnterCountdown()
    {
        _state = States.Countdown;
        InsideScreen!.Message.Text = "Counting Down...";
        var countdownIntervalSeconds = SkipIntro ? 0.1f : 1f;

        this.AddTimer(1 * countdownIntervalSeconds).Timeout += () =>
        {
            AudioManager?.Play(Sounds.Countdown_CountdownSoundEffect8Bit);
            InsideScreen.Message.Text = "Starting in 3...";
        };
        this.AddTimer(2 * countdownIntervalSeconds).Timeout += () => { InsideScreen.Message.Text = "Starting in 2..."; };
        this.AddTimer(3 * countdownIntervalSeconds).Timeout += () => { InsideScreen.Message.Text = "Starting in 1..."; };
        this.AddTimer(4 * countdownIntervalSeconds).Timeout += () => { InsideScreen.Message.Text = "Go!"; };
        this.AddTimer(5 * countdownIntervalSeconds).Timeout += EnterPlay;
    }

    private void EnterPlay()
    {
        _state = States.Play;
        AudioManager?.PlayMusic(Sounds.Music_Competitive_CreepyDevilDance, fadeTime: 0.2);
    }

    private void ProcessTimeBasedEvents()
    {
        var remainingTimeSeconds = _gameConfig.GameDurationSeconds - _gameTimeElapsed;
        if (remainingTimeSeconds <= _gameConfig.RushModeEnableRemainingSeconds)
        {
            _rushModeActivated = true;
        }
        InsideScreen!.TimeDisplay.Pulse = 0 <= remainingTimeSeconds && _rushModeActivated;
        InsideScreen.TimeDisplay.Update(remainingTimeSeconds);

        if (remainingTimeSeconds <= 0)
        {
            EnterGameOver();
            return;
        }

        // TODO - disabling early game over temporarily to test game play experience with just simply letting the time run to completion
        // // Determine if game should finish early - due to not enough space to spawn more tiles
        // var configSpawnCount = _gameConfig.PlayerTargetTileConfig.SpawnCount;
        // var spawnCountPerPlayer = _rushModeActivated ? configSpawnCount * 2 : configSpawnCount;
        // var spawnCountTotal = spawnCountPerPlayer * _playerCount;
        // var capturedTilesCount = _playerTilesCaptured
        //     .Select(t => t.Count)
        //     .Sum();
        //
        // var totalPlayingFieldPositions = _grid.Width * _grid.Height;
        // var availablePositions = totalPlayingFieldPositions - capturedTilesCount;
        // if (availablePositions < spawnCountTotal)
        // {
        //     EnterGameOver();
        //     return;
        // }

        SpawnPlayerTargets();
    }

    private void ProcessPlayerActions()
    {
        var targetPositions = _playerTargetTiles
            .SelectMany(p => p)
            .Select(p => GridSpecs.ToGridPosition(p.GlobalPosition))
            .ToList();

        foreach (var (x, y) in TakePressed())
        {
            var pressedPosition = new Point(x, y);
            if (!GridSpecs.PointWithinGrid(pressedPosition)) continue;
            // Ignore if pressed is not one of the target nodes
            if (!targetPositions.Contains(pressedPosition)) continue;

            // Ignore if cannot find pressed polygon
            var pressedPolygon = _playingFieldPolygons[x, y];
            if (pressedPolygon == null) continue;

            // Determine player color
            var playerIndex = _playerTargetTiles
                .ToList()
                .FindIndex(t => t.Contains(pressedPolygon));
            if (playerIndex == -1) continue;

            var playerColor = PlayerColors[playerIndex];
            var darkenedPlayerColor = playerColor.Darkened(_playerColorDarkness);
            FlashColor(pressedPolygon, _uncapturedColor, playerColor, darkenedPlayerColor);

            var pitchScale = float.Lerp(0.7f, 1, (float)playerIndex / _playerTargetTiles.Length);
            AudioManager?.Play(Sounds.Positive_ClassicGameActionPositive5, pitchScale);

            // Get positions in radius of pressed tile
            var radiusPositions = GridHelper.GetPositionsByRadius(_grid, pressedPosition.X, pressedPosition.Y, 1);
            List<Polygon2D> radiusPolygonsExceptPressed = [];
            foreach (var rp in radiusPositions)
            {
                if (rp == pressedPosition) continue;

                var radiusPolygon = _playingFieldPolygons.GetValue(rp.X, rp.Y) as Polygon2D;
                if (radiusPolygon == null) continue;
                radiusPolygonsExceptPressed.Add(radiusPolygon);
            }

            // Capture each radius polygon
            foreach (var radiusPolygon in radiusPolygonsExceptPressed)
            {
                if (_playerTargetTiles[playerIndex].Contains(radiusPolygon))
                {
                    // Do nothing if adjacent target tile is of same color - require the player to capture that too
                    continue;
                }

                // If captured by other players, make sure to remove from tracking
                // Note - dictionaries do not allow null returns if checking for values even with FirstOrDefault
                var capturedIndex = _playerTilesCaptured
                    .Where(kvp => kvp.Value.Contains(radiusPolygon))
                    .Select(kvp => (KeyValuePair<int, List<Polygon2D>>?)kvp)
                    .FirstOrDefault()?
                    .Key;
                if (capturedIndex.HasValue)
                    _playerTilesCaptured[capturedIndex.Value].Remove(radiusPolygon);

                // If another player's active targets, make sure to remove from tracking
                var targetIndex = _playerTargetTiles.ToList()
                    .FindIndex(polygons => polygons.Contains(radiusPolygon));
                if (targetIndex != -1)
                {
                    var tweens = _playerTargetTweens[targetIndex];
                    if (tweens.TryGetValue(radiusPolygon, out var tween))
                    {
                        tween.Stop();
                        tweens.Remove(radiusPolygon);
                    }
                    _playerTargetTiles[targetIndex].Remove(radiusPolygon);
                }

                FlashColor(radiusPolygon, _uncapturedColor, playerColor, darkenedPlayerColor);
                radiusPolygon.Color = darkenedPlayerColor;

                if (!_playerTilesCaptured[playerIndex].Contains(radiusPolygon))
                    _playerTilesCaptured[playerIndex].Add(radiusPolygon);
            }

            // Update pressed tile to the player's color
            pressedPolygon.Color = darkenedPlayerColor;

            // Stop the animation of pressed tile
            var playerTweens = _playerTargetTweens[playerIndex];
            if (playerTweens.TryGetValue(pressedPolygon, out var playerTween))
            {
                playerTween.Stop();
                playerTweens.Remove(pressedPolygon);
            }
            _playerTargetTiles[playerIndex].Remove(pressedPolygon);
            _playerTilesCaptured[playerIndex].Add(pressedPolygon);
        }
    }

    private void EnterGameOver()
    {
        _state = States.GameOver;
        AudioManager?.StopAll();
        InsideScreen!.TimeDisplay.TimesUp();

        UpdateInGameMessage();

        // Stop any remaining animations
        foreach (var tween in _playerTargetTweens.SelectMany(t => t.Values))
        {
            tween.Stop();
        }

        for (var pi = 0; pi < _playerCount; pi++)
        {
            _playerTargetTweens[pi] = [];
        }

        // Slowly fade out each losing color
        var winningCount = 0;
        List<int> winningPlayerIndexes = [];
        for (var pi = 0; pi < _playerCount; pi++)
        {
            var tilesCaptured = _playerTilesCaptured[pi];
            var tilesCapturedCount = tilesCaptured.Count;

            if (tilesCapturedCount == winningCount)
            {
                winningPlayerIndexes.Add(pi);
            }
            else if (tilesCapturedCount > winningCount)
            {
                winningPlayerIndexes.Clear();
                winningPlayerIndexes.Add(pi);
                winningCount = tilesCapturedCount;
            }
        }

        var winningColors = PlayerColors
            .Where((_, i) => winningPlayerIndexes.Contains(i))
            .ToList();

        this.AddTimer(5).Timeout += () =>
        {
            var rand = new Random(_randomSeed);
            var randomOrderedNodes = _playingField
                .GetChildrenByType<Polygon2D>()
                .Where(poly => !winningColors.Select(c => c.Darkened(_playerColorDarkness)).Contains(poly.Color))
                .Where(poly => poly.Color != _uncapturedColor)
                .OrderBy(_ => rand.Next())
                .ToList();

            const float duration = 5f;
            var removeInterval = duration / randomOrderedNodes.Count;
            for (var i = 0; i < randomOrderedNodes.Count; i++)
            {
                var polygon = randomOrderedNodes[i];
                var delay = removeInterval * i;
                var maxPitchScale = randomOrderedNodes.Count * 0.1f;

                this.AddTimer(delay).Timeout += () =>
                {
                    var pitchScale = float.Lerp(1, 3, delay / maxPitchScale);
                    FlashColor(polygon, Colors.White, Colors.White, _uncapturedColor);
                    AudioManager?.Play(Sounds.Neutral_Button, pitchScale);
                };

                // Completion Sound
                if (i == randomOrderedNodes.Count - 1)
                {
                    this.AddTimer(delay + 2).Timeout += () =>
                    {
                        AudioManager?.Play(Sounds.GameWin_YouWinSequence1);
                    };
                }

            }
        };
    }

    public override void _Process(double delta)
    {
        if (!_isGameInitialized) return;

        switch (_state)
        {
            case States.Flourish:
                ProcessFlourish();
                break;
            case States.PreReveal:
                ProcessPreReveal();
                break;
            case States.SelectColor:
                ProcessSelectColor();
                UpdateInGameMessage();
                break;
            case States.Play:
                _gameTimeElapsed += delta;
                ProcessPlayerActions();
                ProcessTimeBasedEvents();
                UpdateInGameMessage();
                break;
        }
    }

    private void SpawnPlayerTargets()
    {
        // If all players still have their last cycle targets still active, ignore this cycle to avoid unnecessary loops
        if (_playerTargetTiles.All(t => t.Count > 0))
            return;

        var configSpawnCount = _gameConfig.PlayerTargetTileConfig.SpawnCount;
        var spawnCountPerPlayer = _rushModeActivated ? configSpawnCount * 2 : configSpawnCount;
        var totalTargetsToSpawn = spawnCountPerPlayer * _playerCount;

        var availableTiles = _playingFieldPolygons.Cast<Polygon2D>()
            .Where(p => p != null &&
                        _playerTilesCaptured.SelectMany(kvp => kvp.Value).All(t => t != p) &&
                        _playerTargetTiles.SelectMany(t => t).All(t => t != p))
            .ToList();

        var totalAvailablePositions = availableTiles.Count;
        var canSpawnAllTargets = totalAvailablePositions >= totalTargetsToSpawn;

        // If can't spawn all targets, then prioritize lowest scoring players
        var playerIndexes = Enumerable.Range(0, _playerTilesCaptured.Count).ToList();
        if (!canSpawnAllTargets)
        {
            playerIndexes.Sort((a, b) => _playerTilesCaptured[a].Count.CompareTo(_playerTilesCaptured[b].Count));
            playerIndexes.Reverse();
        }

        List<Polygon2D> prioritySpawnTiles = [];
        // First get a list of priority tiles that should be used if spawned targets exceed available positions
        for(var index = 0; index < playerIndexes.Count; index++)
        {
            var pi = playerIndexes[index];

            // How much the player's total percentage that will be added to the priority tiles will be reduced
            // The lower the index value, the higher the priority. This will make sure the top player will be reduced by 0%
            // Therefore 100% of top player's captured tiles will be added to the priority list
            // Subsequent players will have their percentages halved each time, making sure the bottom player will have least chance that an enemy tile will spawn on their tiles
            const double reductionStep = 0.5;
            var reductionPercentage = index * reductionStep;
            var playerPriorityPercentage = 1 - reductionPercentage;

            var captured = _playerTilesCaptured[pi];
            var totalPriorityTiles = (int)(playerPriorityPercentage * captured.Count);

            var playerPriorityTiles = captured.Take(totalPriorityTiles);
            prioritySpawnTiles.AddRange(playerPriorityTiles);
        }

        foreach (var pi in playerIndexes)
        {
            var playerColor = PlayerColors[pi];
            var playerTargets = _playerTargetTiles[pi];

            // Don't spawn for player if their targets are still active or animating (indication it's about to disappear)
            if (playerTargets.Count > 0) continue;

            // Spawn targets per player
            // If there are less available tiles than expected targets to spawn, then randomly select from available tiles and the top players' captured tiles
            // Otherwise, just randomize from all available tiles within the grid
            List<Point> targetTilePositions;
            if (spawnCountPerPlayer > totalAvailablePositions)
            {
                // Also add the available tiles on priority tiles
                prioritySpawnTiles.AddRange(availableTiles);
                targetTilePositions = _rand.NextFromList(prioritySpawnTiles
                        .Select(t => GridSpecs.ToGridPosition(t.GlobalPosition)).ToList(), spawnCountPerPlayer);
            }
            else if(spawnCountPerPlayer == totalAvailablePositions)
            {
                targetTilePositions = _rand.NextFromList(prioritySpawnTiles
                    .Select(t => GridSpecs.ToGridPosition(t.GlobalPosition)).ToList(), spawnCountPerPlayer);
            }
            else
            {
                targetTilePositions = _rand.NextFromList(availableTiles
                    .Select(t => GridSpecs.ToGridPosition(t.GlobalPosition)).ToList(), spawnCountPerPlayer);
            }

            foreach (var targetTilePosition in targetTilePositions)
            {
                var targetTileNode = _playingField
                    .GetChildrenByType<Polygon2D>()
                    .FirstOrDefault(p => GridSpecs.ToGridPosition(p.GlobalPosition) == targetTilePosition);
                if (targetTileNode == null)
                {
                    continue;
                }

                targetTileNode.Color = playerColor;
                _playerTargetTiles[pi].Add(targetTileNode);

                // If overriding another player's captured tile, remove from tracking
                var persistPi = pi;
                var otherPlayerIndex = _playerTilesCaptured.ToList().FindIndex(kvp => kvp.Value.Contains(targetTileNode));
                if (otherPlayerIndex != -1)
                {
                    _playerTilesCaptured[otherPlayerIndex].Remove(targetTileNode);
                }

                // Start animating tile to indicate target is about to disappear
                this.AddTimer(3f).Timeout += () =>
                {
                    if (_state == States.GameOver) return;
                    if (!_playerTargetTiles[persistPi].Contains(targetTileNode)) return;

                    var totalDuration = 2f;
                    var singleTweenDuration = 0.25f;
                    var loopTotal = (int)(totalDuration / singleTweenDuration);
                    var tween = FlashColorLoop(targetTileNode, _uncapturedColor, playerColor, singleTweenDuration, 0f, loopTotal);
                    _playerTargetTweens[persistPi].TryAdd(targetTileNode, tween);
                };
                // Remove target
                this.AddTimer(5f).Timeout += () =>
                {
                    if (_state == States.GameOver) return;
                    if (!_playerTargetTiles[persistPi].Contains(targetTileNode)) return;
                    if (_playerTargetTweens[persistPi].TryGetValue(targetTileNode, out var tween))
                    {
                        tween.Stop();
                        _playerTargetTweens[persistPi].Remove(targetTileNode);
                    }
                    _playerTargetTiles[persistPi].Remove(targetTileNode);
                    targetTileNode.Color = _uncapturedColor;
                };
                totalAvailablePositions--;
            }
        }
    }

    private void UpdateInGameMessage()
    {
        var initialMessage = "Step on tiles to gain territory!";
        var rushTimeMessage = "Targets doubled! Quick!";
        var actualMessage = _state == States.GameOver
            ? "Final Scores"
            : _rushModeActivated
                ? rushTimeMessage
                : initialMessage;

        var totalCaptured = _playerTilesCaptured.Select(kvp => kvp.Value.Count).Sum();
        var totalPlayingFieldNodes = _playingField.GetChildCount();

        bool playerDominated = false;
        int dominatingPlayer = -1;
        var playerScoreString = "";
        for (var pi = 0; pi < _playerCount; pi++)
        {
            if (_playerTileSelected.Count == _playerCount && _state == States.SelectColor)
            {
                var isPlayerReady = _playerTileSelected.ElementAt(pi).Value;
                var isPlayerReadyString = isPlayerReady ? "Ready" : "Not Ready";
                playerScoreString += $"Player {pi + 1}: {isPlayerReadyString}\n";
            }
            else
            {
                var capturedPolygons = _playerTilesCaptured[pi];
                playerScoreString += $"Player {pi + 1}: {capturedPolygons.Count}\n";

                if (capturedPolygons.Count == totalPlayingFieldNodes)
                {
                    playerDominated = true;
                    dominatingPlayer = pi + 1;
                }
            }
        }
        if (playerDominated)
            playerScoreString = $"Player {dominatingPlayer} has dominated the grid area!";

        InsideScreen!.Message.Text = $"{actualMessage}\n\n" +
                        $"Board Coverage: {totalCaptured}/{totalPlayingFieldNodes}\n" +
                        $"{playerScoreString}";
    }
}
