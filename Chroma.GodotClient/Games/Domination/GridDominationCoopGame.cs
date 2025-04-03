using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using MysticClue.Chroma.GodotClient.Audio;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.GameLogic.Physics;
using MysticClue.Chroma.GodotClient.Games.Domination.Config;
using MysticClue.Chroma.GodotClient.Games.Domination.Controllers;
using MysticClue.Chroma.GodotClient.Games.Domination.Models;
using GridHelper = MysticClue.Chroma.GodotClient.Games.Domination.GridDominationGridHelper;
using Point = System.Drawing.Point;

namespace MysticClue.Chroma.GodotClient.Games.Domination;

/// <summary>
/// Cooperative grid game where players need to capture as many tiles as possible within the time limit.
/// Players are playing against a bot which randomly moves within the grid stealing captured tiles.
/// </summary>
public partial class GridDominationCoopGame : GridGame
{
    private Color _enemyColor = Colors.Red;
    private readonly Color _capturedColor = Colors.Green;
    private readonly Color _uncapturedColor = Colors.Transparent;
    private readonly Color _goldTileColor = Colors.Gold;
    private readonly Color _vulnerableEnemyColor = Colors.Pink;
    private readonly Color _stunnedEnemyColor = Colors.Brown;
    private readonly Color _powerTileColor = Colors.Blue;

    private readonly GridDominationScoreController _scoreController;
    private readonly GridDominationEnemyController _enemyController;
    private readonly GridDominationGridHelper _gridHelper;
    private readonly ResolvedGridSpecs _grid;
    private readonly GridDominationConfig.GameCoopConfig _gameCoopConfig;
    private readonly double _gameSpeed;
    private readonly int _playerCount;
    private readonly int _randomSeed;

    private bool _spawnPowerTileOnCooldown;
    private bool _spawnGoldTileOnCooldown;
    private bool _turnEnemyVulnerableOnCooldown;
    private bool _rushModeActivated;

    private double _gameTimeElapsed;

    // One child Polygon2D corresponding to each object defined in _gameState.
    private Node2D _enemyNodes = new() { Name = "Enemies" };
    private Node2D _targetNodes = new() { Name = "Targets" };

    // Extra layers for animations.
    private Node2D _topLayer = new() { Name = "TopLayer" };

    private enum States { Flourish, PreReveal, RevealEnemy, RevealInitialCaptured, Countdown, Play, GameOver };
    private States _state = States.Flourish;
    public override string StateString => _state.ToString();

    private bool _isGameInitialized;

    protected override IReadOnlyList<Sounds> UsedSounds =>
    [
        Sounds.Music_Competitive_CreepyDevilDance,
        Sounds.Positive_GameBonus,
        Sounds.GameWin_YouWinSequence1,
        Sounds.Flourish_90sGameUi4,
        Sounds.Flourish_CuteLevelUp2,
        Sounds.Countdown_CountdownSoundEffect8Bit,
        Sounds.Positive_90sGameUi6,
        Sounds.Negative_HurtC08,
        Sounds.Negative_StabF01
    ];

    public GridDominationCoopGame(
        ResolvedGridSpecs grid,
        GridDominationConfig.GameCoopConfig gameCoopConfig,
        GridDominationEnemyController enemyController,
        GridDominationGridHelper gridHelper,
        GridDominationScoreController scoreController,
        double gameSpeed,
        int playerCount,
        int randomSeed) : base(grid)
    {
        GDPrint.LogState(gameCoopConfig, enemyController, gridHelper, scoreController, gameSpeed, randomSeed);

        _enemyController = enemyController;
        _gridHelper = gridHelper;
        _scoreController = scoreController;
        _gameSpeed = gameSpeed;
        _playerCount = playerCount;
        _randomSeed = randomSeed;
        _grid = grid;
        _gameCoopConfig = gameCoopConfig;
        SetupGame();
        EnterFlourish();
    }

    private void EnterFlourish()
    {
        _state = States.Flourish;
        _targetNodes.Hide();
        _enemyNodes.Hide();
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
        EnterCapturedTilesReveal();
    }

    private void EnterRevealEnemy()
    {
        _state = States.RevealEnemy;

        float enemyPolygonRevealIntervalSeconds = SkipIntro ? 0.1f : 1f;
        this.AddTimer(enemyPolygonRevealIntervalSeconds).Timeout += () =>
        {
            AudioManager?.Play(Sounds.Negative_StabF01);
            for (var i = 0; i < _enemyNodes.GetChildCount(); i++)
            {
                foreach (var polygon in _enemyNodes.GetChild(i).GetChildrenByType<Polygon2D>())
                {
                    EaseInFlashWhite(polygon);
                }
            }
            _enemyNodes.Show();
            this.AddTimer(2).Timeout += EnterCountdown;
        };
    }

    private void EnterCapturedTilesReveal()
    {
        _state = States.RevealInitialCaptured;
        InsideScreen!.Message.Text = "Here are your starting tiles!";

        float capturedTilesRevealSeconds = SkipIntro ? 0.1f : 2f;
        this.AddTimer(capturedTilesRevealSeconds).Timeout += () =>
        {
            AudioManager?.Play(Sounds.Positive_GameBonus);
            var rand = new Random(_randomSeed);
            double durationSeconds = SkipIntro ? 0.1 : 2;
            var totalInitialCapturedTiles = _gameCoopConfig.InitialCapturedTilesPercentage * _targetNodes.GetChildCount();
            var capturedTilesRevealIntervalSeconds = durationSeconds / totalInitialCapturedTiles;
            var polygonIndex = 0;
            for (var i = 0; i < _targetNodes.GetChildCount(); i++)
            {
                var r = rand.Next(0, 100);
                var isCaptured = r <= _gameCoopConfig.InitialCapturedTilesPercentage * 100;

                if (!isCaptured) continue;

                var tileArea = _targetNodes.GetChild(i);
                foreach (var polygon in tileArea.GetChildrenByType<Polygon2D>())
                {
                    var delay = polygonIndex * capturedTilesRevealIntervalSeconds;
                    polygon.Color = _capturedColor;

                    EaseInFlashWhite(polygon, delay);
                    polygonIndex++;
                }
            }

            _targetNodes.Show();
            this.AddTimer(durationSeconds + 2).Timeout += EnterRevealEnemy;
        };
    }

    private List<Node2D> TryGetClosestCapturedNodes(Point fromPosition, int? totalNodesToGet = 1)
    {
        totalNodesToGet ??= 1;
        var fromPositionAsVector = new Vector2(fromPosition.X, fromPosition.Y);
        var nodesToReturn = _targetNodes
            .GetChildrenByType<Polygon2D>()
            .Where(p => p.Color == _capturedColor)
            .OrderBy(p => p.GlobalPosition.DistanceTo(fromPositionAsVector))
            .Select(p => p.GetParent<Node2D>())
            .Take(totalNodesToGet.Value)
            .ToList();

        return nodesToReturn;
    }

    // TODO - becoming more apparent to me that I should probably make GridDominationEnemy inherit from Node2D. Need to think about it more.
    private void AssignNewRoute(GridDominationEnemy enemy, Node2D enemyNode)
    {
        var enemyPosition = new Point(
            (int)enemyNode.GlobalPosition.X,
            (int)enemyNode.GlobalPosition.Y);
        var closestCapturedNodes = TryGetClosestCapturedNodes(enemyPosition, 2);
        if (closestCapturedNodes.Count > 0)
        {
            var currentPosition = enemyPosition;
            List<Grid2DMovement> movements = [];
            foreach (var capturedNode in closestCapturedNodes)
            {
                var destinationPosition = new Point(
                    (int)capturedNode.GlobalPosition.X,
                    (int)capturedNode.GlobalPosition.Y);
                movements.AddRange(GridHelper.GetMovementsToPosition(_grid, currentPosition, destinationPosition));
                currentPosition = destinationPosition;
            }
            _enemyController.SetEnemyRoute(enemy.Name, movements);
        }
        else
        {
            var foundValidMovement = false;
            // Valid movement is only found if the steps is not 0.
            // 0 can be a result of a randomized step towards another edge
            // (i.e., if node is in bottom-left corner moving to the left. Randomizer can result to a movement to the bottom therefore 0 valid steps)
            while (!foundValidMovement)
            {
                var randomMovement = _gridHelper.GetRandomMovement(_grid, enemyPosition, enemy.LastDirection);
                if (randomMovement == null) continue;

                _enemyController.SetEnemyRoute(enemy.Name, [randomMovement]);
                foundValidMovement = true;
            }
        }
    }

    private void InitiatePeriodicMovement()
    {
        var timerIntervalSeconds = _rushModeActivated
            ? _gameCoopConfig.EnemyConfig.MovementIntervalSeconds / 2
            : _gameCoopConfig.EnemyConfig.MovementIntervalSeconds;
        // A recursive call of timed game steps
        this.AddTimer(timerIntervalSeconds).Timeout += () =>
        {
            // Stop the game stepping once state leaves Play
            if (_state != States.Play) return;

            var enemies = _enemyController.GetEnemiesReadonly();
            for (var i = 0; i < enemies.Count; ++i)
            {
                var enemy = enemies[i];
                var enemyNode = _enemyNodes.GetChild<Node2D>(i);
                if (enemyNode is Area2D enemyArea)
                {
                    var polygons = enemyArea.GetChildrenByType<Polygon2D>();
                    var isEnemyStunned = polygons.Any(p => p.Color == _stunnedEnemyColor);
                    if (isEnemyStunned) continue;
                }

                if (_enemyController.IsEnemyRouteComplete(enemy.Name))
                {
                    AssignNewRoute(enemy, enemyNode);
                }

                var currentMovement = _enemyController.GetEnemyCurrentMovement(enemy.Name);
                if (currentMovement != null)
                {
                    // Get vector based on movement direction
                    var nextStep = currentMovement.Direction.ToVector2();

                    var nextPosition = enemyNode.GlobalPosition + nextStep;
                    if (nextPosition.X >= _grid.Width || nextPosition.Y >= _grid.Height)
                        continue;

                    // TODO - logic to avoid enemy nodes overlapping removed for now. This sometimes cause traffic. Need to think about this more.
                    // var isNextPositionAvailable = true;
                    // foreach (var en in _enemyNodes.GetChildrenByType<Node2D>())
                    // {
                    //     if (en.GlobalPosition.X == nextPosition.Value.X && en.GlobalPosition.Y == nextPosition.Value.Y)
                    //     {
                    //         isNextPositionAvailable = false;
                    //         break;
                    //     }
                    // }
                    //
                    // if (!isNextPositionAvailable)
                    // {
                    //     _gameController.SetEnemyRoute(enemy.Name, []);
                    //     continue;
                    // }

                    enemyNode.Position += nextStep;
                    _enemyController.DecrementCurrentMovementSteps(enemy.Name);
                    enemy.LastDirection = currentMovement.Direction;
                    _enemyController.TryRemoveCurrentMovementIfCompleted(enemy.Name);
                }
                PerformEnemyTileCaptureIfOverlapping(enemyNode);
            }
            InitiatePeriodicMovement();
        };
    }

    private void PerformEnemyTileCaptureIfOverlapping(Node2D enemyNode)
    {
        if (enemyNode is not Area2D enemyArea) return;

        // Update color of any nodes that are overlapping with the enemies
        foreach (var overlappingArea in enemyArea.GetOverlappingAreas())
        {
            if (overlappingArea.GetParent() != _targetNodes) continue;
            var overlappingPolygons = overlappingArea.GetChildrenByType<Polygon2D>();
            foreach (var poly in overlappingPolygons)
            {
                if (poly.Color != _capturedColor) continue;

                poly.Color = _uncapturedColor;
            }
        }
        UpdateInGameMessage();
    }

    private void EnterCountdown()
    {
        _state = States.Countdown;
        InsideScreen!.Message.Text = "Counting Down...";
        float countdownIntervalSeconds = SkipIntro ? 0.1f : 1f;
        this.AddTimer(1 * countdownIntervalSeconds).Timeout += () =>
        {
            AudioManager?.Play(Sounds.Countdown_CountdownSoundEffect8Bit);
            InsideScreen!.Message.Text = "Starting in 3...";
        };
        this.AddTimer(2 * countdownIntervalSeconds).Timeout += () => { InsideScreen!.Message.Text = "Starting in 2..."; };
        this.AddTimer(3 * countdownIntervalSeconds).Timeout += () => { InsideScreen!.Message.Text = "Starting in 1..."; };
        this.AddTimer(4 * countdownIntervalSeconds).Timeout += () => { InsideScreen!.Message.Text = "Go!"; };
        this.AddTimer(5 * countdownIntervalSeconds).Timeout += EnterPlay;
    }

    private void EnterPlay()
    {
        _state = States.Play;
        AudioManager?.PlayMusic(Sounds.Music_Competitive_CreepyDevilDance, fadeTime: 0.2);
        UpdateInGameMessage();
        InitiatePeriodicMovement();
        TakePressed();
    }

    private void ProcessPlayWithTimeBasedEvents()
    {
        var remainingTimeSeconds = _gameCoopConfig.GameDurationSeconds - _gameTimeElapsed;
        if (remainingTimeSeconds <= _gameCoopConfig.RushModeEnableRemainingSeconds)
        {
            _rushModeActivated = true;
        }
        InsideScreen!.TimeDisplay.Pulse = 0 <= remainingTimeSeconds && _rushModeActivated;
        InsideScreen!.TimeDisplay.Update(remainingTimeSeconds);

        if (remainingTimeSeconds <= 0)
        {
            EnterGameOver();
            return;
        }

        var gameTotalDuration = _gameCoopConfig.GameDurationSeconds;
        if (!_spawnGoldTileOnCooldown &&
            remainingTimeSeconds <= gameTotalDuration - _gameCoopConfig.GoldTileConfig.SpawnEnableDelaySeconds)
            SpawnGoldTile(_gameCoopConfig.GoldTileConfig.SpawnCount);

        if (!_spawnPowerTileOnCooldown &&
            remainingTimeSeconds <= gameTotalDuration - _gameCoopConfig.PowerTileConfig.SpawnEnableDelaySeconds)
            SpawnPowerTiles(_gameCoopConfig.PowerTileConfig.SpawnCount);

        if (!_turnEnemyVulnerableOnCooldown &&
            remainingTimeSeconds <= gameTotalDuration - _gameCoopConfig.EnemyConfig.VulnerableEnableDelaySeconds)
            TurnEnemiesVulnerableToStun();
    }

    // TODO - need to refactor, not really using physics
    private void ProcessPlayWithPhysics()
    {
        // Check for presses since last time.
        foreach (var point in TakePressed())
        {
            FlashSquare(_topLayer, point, Colors.Orange, _capturedColor, flashTwice: false);

            // Target Nodes to capture
            var targetTileAreas = _targetNodes.GetChildrenByType<Area2D>();
            foreach (var tileArea in targetTileAreas)
            {
                var polygons = tileArea
                    .GetChildrenByType<Polygon2D>()
                    .Where(t => (int)t.GlobalPosition.X == point.X && (int)t.GlobalPosition.Y == point.Y);

                List<Polygon2D> radiusPolygonsToCapture = [];
                List<Polygon2D> crossPolygonsToCapture = [];
                foreach (var polygon in polygons)
                {
                    var x = (int)polygon.GlobalPosition.X;
                    var y = (int)polygon.GlobalPosition.Y;

                    // Get radius polygons
                    var radiusPositions = GridHelper.GetPositionsByRadius(_grid, x, y, _gameCoopConfig.CaptureSplashRadius);
                    var radiusPolygons = _targetNodes
                        .GetChildrenByType<Polygon2D>()
                        .Where(target => radiusPositions
                            .Any(pos => pos.X == (int)target.GlobalPosition.X && pos.Y == (int)target.GlobalPosition.Y));
                    radiusPolygonsToCapture.AddRange(radiusPolygons.ToList());

                    // Get cross directional polygons
                    if (polygon.Color == _powerTileColor)
                    {
                        AudioManager?.Play(Sounds.Flourish_CuteLevelUp2);
                        _scoreController.AddPowerTileCount();
                        var crossPositions = GridHelper.GetPositionsByCrossDirection(_grid, x, y);
                        var crossPolygons = _targetNodes
                            .GetChildrenByType<Polygon2D>()
                            .Where(target => crossPositions
                                .Any(pos => pos.X == (int)target.GlobalPosition.X && pos.Y == (int)target.GlobalPosition.Y));

                        crossPolygonsToCapture.AddRange(crossPolygons);
                    }

                    if (polygon.Color == _capturedColor) continue;

                    if (polygon.Color == _goldTileColor)
                    {
                        AudioManager?.Play(Sounds.Positive_90sGameUi6);
                        _scoreController.AddGoldTileCount();
                    }

                    polygon.Color = _capturedColor;
                    _scoreController.AddNormalTileCount();
                }

                foreach (var radiusPolygonToCapture in radiusPolygonsToCapture.ToList())
                {
                    // If tile is a special tile, do not capture
                    if (radiusPolygonToCapture.Color == _goldTileColor || radiusPolygonToCapture.Color == _powerTileColor)
                        continue;

                    // If already captured, ignore
                    if (radiusPolygonToCapture.Color == _capturedColor)
                        continue;

                    radiusPolygonToCapture.Color = _capturedColor;
                    _scoreController.AddNormalTileCount();

                    if (_rushModeActivated) _scoreController.AddRushTileCount();
                }

                var index = 1;
                foreach (var crossPolygonToCapture in crossPolygonsToCapture)
                {
                    // If tile is a special tile, do not capture
                    if (crossPolygonToCapture.Color == _goldTileColor || crossPolygonToCapture.Color == _powerTileColor)
                        continue;

                    FlashColor(crossPolygonToCapture, _powerTileColor, _powerTileColor, _capturedColor, index * 0.01F);

                    // If already captured, ignore
                    if (crossPolygonToCapture.Color == _capturedColor)
                        continue;

                    crossPolygonToCapture.Color = _capturedColor;
                    _scoreController.AddNormalTileCount();

                    if (_rushModeActivated) _scoreController.AddRushTileCount();
                    index++;
                }
            }

            // Enemy Nodes to stun
            var enemyTileAreas = _enemyNodes.GetChildrenByType<Area2D>();
            foreach (var tileArea in enemyTileAreas)
            {
                var polygons = tileArea
                    .GetChildrenByType<Polygon2D>()
                    .Where(t => (int)t.GlobalPosition.X == point.X && (int)t.GlobalPosition.Y == point.Y);

                Dictionary<Node2D, List<Polygon2D>> enemyNodes = [];
                foreach (var polygon in polygons)
                {
                    var enemyNode = polygon.GetParent<Node2D>();
                    enemyNodes.TryAdd(enemyNode, []);
                    enemyNodes[enemyNode].Add(polygon);
                }

                foreach (var kvp in enemyNodes)
                {
                    var enemyNode = kvp.Key;
                    var enemyPolygons = kvp.Value;

                    // This check is useful once we allow for enemy nodes that is bigger than 1x1
                    var isEnemyVulnerable = enemyPolygons.Any(p => p.Color == _vulnerableEnemyColor);
                    if (!isEnemyVulnerable) continue;
                    AudioManager?.Play(Sounds.Negative_HurtC08);
                    _scoreController.AddEnemyStunCount();
                    foreach (var polygon in enemyPolygons)
                    {
                        if (polygon.Color == _vulnerableEnemyColor)
                        {
                            polygon.Color = _stunnedEnemyColor;
                            this.AddTimer(5).Timeout += () => { polygon.Color = _enemyColor; };
                        }
                    }
                }
            }
        }

        UpdateInGameMessage();
    }

    private void EnterGameOver()
    {
        _state = States.GameOver;
        AudioManager?.StopAll();
        InsideScreen!.Message.Text = "Game Over!";
        ShowFinalGameScore();

        foreach (var enemyNode in _enemyNodes.GetChildrenByType<Polygon2D>())
        {
            FlashColorLoop(enemyNode, Colors.White, Colors.Red, 0.1f, 0f, 3);
        }

        AudioManager?.PlayMusic(Sounds.GameWin_YouWinSequence1);
        this.AddTimer(30).Timeout += () =>
        {
            SetupGame();
            EnterFlourish();
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
            case States.Play:
                _gameTimeElapsed += delta;
                ProcessPlayWithTimeBasedEvents();
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isGameInitialized) return;
        switch (_state)
        {
            case States.Play:
                ProcessPlayWithPhysics();
                break;
        }
    }

    private void SpawnPowerTiles(int totalToSpawn)
    {
        if (_spawnPowerTileOnCooldown) return;
        _spawnPowerTileOnCooldown = true;
        for (var i = 0; i < totalToSpawn; i++)
        {
            var powerTilePosition = _gridHelper.GetRandomPositionWithinGrid(_grid);
            var powerTileNode = _targetNodes
                .GetChildrenByType<Polygon2D>()
                .FirstOrDefault(p => (int)p.GlobalPosition.X == powerTilePosition.X && (int)p.GlobalPosition.Y == powerTilePosition.Y);
            if (powerTileNode == null) return;
            powerTileNode.Color = _powerTileColor;
            this.AddTimer(_gameCoopConfig.PowerTileConfig.SpawnDurationSeconds).Timeout += () =>
            {
                if (powerTileNode.Color == _capturedColor) return;
                powerTileNode.Color = _uncapturedColor;
            };
        }

        this.AddTimer(_gameCoopConfig.PowerTileConfig.SpawnIntervalSeconds).Timeout += () =>
        {
            _spawnPowerTileOnCooldown = false;
        };
    }

    private void SpawnGoldTile(int totalToSpawn)
    {
        if (_spawnGoldTileOnCooldown) return;
        _spawnGoldTileOnCooldown = true;
        for (var i = 0; i < totalToSpawn; i++)
        {
            var goldTilePosition = _gridHelper.GetRandomPositionWithinGrid(_grid);
            var goldTileNode = _targetNodes
                .GetChildrenByType<Polygon2D>()
                .FirstOrDefault(p => (int)p.GlobalPosition.X == goldTilePosition.X && (int)p.GlobalPosition.Y == goldTilePosition.Y);
            if (goldTileNode == null) return;
            goldTileNode.Color = _goldTileColor;
            this.AddTimer(_gameCoopConfig.GoldTileConfig.SpawnDurationSeconds).Timeout += () =>
            {
                if (goldTileNode.Color == _capturedColor) return;
                goldTileNode.Color = _uncapturedColor;
            };
        }

        this.AddTimer(_gameCoopConfig.GoldTileConfig.SpawnIntervalSeconds).Timeout += () =>
        {
            _spawnGoldTileOnCooldown = false;
        };
    }

    private void TurnEnemiesVulnerableToStun()
    {
        if (_turnEnemyVulnerableOnCooldown) return;
        _turnEnemyVulnerableOnCooldown = true;
        foreach (var enemyNode in _enemyNodes.GetChildrenByType<Polygon2D>())
        {
            enemyNode.Color = _vulnerableEnemyColor;
        }
        this.AddTimer(_gameCoopConfig.EnemyConfig.VulnerableDurationSeconds).Timeout += () =>
        {
            foreach (var enemyNode in _enemyNodes.GetChildrenByType<Polygon2D>())
            {
                if (enemyNode.Color != _stunnedEnemyColor)
                    enemyNode.Color = _enemyColor;
            }
            this.AddTimer(_gameCoopConfig.EnemyConfig.VulnerableIntervalSeconds).Timeout += () =>
            {
                _turnEnemyVulnerableOnCooldown = false;
            };
        };
    }

    private void SetupGame()
    {
        _scoreController.CreateNewScoreSheet();
        _gameTimeElapsed = 0;

        InsideScreen!.TimeDisplay.Show();
        InsideScreen!.ScoreCounter.Show();
        InsideScreen!.Message.Show();

        for (var x = 0; x < _grid.Width; ++x)
        {
            for (var y = 0; y < _grid.Height; ++y)
            {
                var t = MakePolygonArea(new(1, 1), Colors.Transparent);
                t.Position = new Vector2(x, y);
                t.Name = $"TargetNode_{x}_{y}";
                _targetNodes.AddChild(t);
            }
        }

        // TODO - Hard coded number of players for now. Need to hook this to the player count input once integrated
        var enemies = _enemyController.InitializeEnemiesAsReadonly(_playerCount);
        for (var i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            var startingPosition = _gridHelper.GetRandomPositionWithinGrid(_grid);
            var polygon = MakePolygonArea(enemy.Size, _enemyColor);
            polygon.Position = new Vector2(startingPosition.X, startingPosition.Y);
            polygon.Name = enemy.Name;
            _enemyNodes.AddChild(polygon);

            var enemyNode = _enemyNodes.GetChild<Node2D>(i);

            var globalPosition = new Point(
                (int)enemyNode.GlobalPosition.X,
                (int)enemyNode.GlobalPosition.Y);

            var isMovementValid = false;
            while (!isMovementValid)
            {
                var randomMovement = _gridHelper.GetRandomMovement(_grid, globalPosition);
                if (randomMovement == null) continue;

                _enemyController.SetEnemyRoute(enemy.Name, [randomMovement]);
                isMovementValid = true;
            }
        }

        AddChild(_targetNodes);
        AddChild(_enemyNodes);
        AddChild(Flourish);
        AddChild(_topLayer);
        _isGameInitialized = true;
    }

    private void UpdateInGameMessage()
    {
        var initialMessage = "Step on tiles to earn some points!";
        var rushTimeMessage = "Rush: Double Speed! Triple Points!";
        var actualMessage = _rushModeActivated ? rushTimeMessage : initialMessage;

        var totalCaptured = 0;
        var targetTileAreas = _targetNodes.GetChildrenByType<Area2D>();
        foreach (var tileArea in targetTileAreas)
        {
            var polygons = tileArea.GetChildrenByType<Polygon2D>();
            totalCaptured += polygons.Where(p => p.Color == _capturedColor).ToList().Count;
        }

        InsideScreen!.ScoreCounter.Update(_scoreController.CalculateTotalPoints());
        InsideScreen!.Message.Text =
            $"{actualMessage}\n\n" +
            $"Current Captured:  {totalCaptured}/{_grid.Width * _grid.Height} tiles\n";
    }

    private void ShowFinalGameScore()
    {
        var totalCaptured = 0;
        var targetTileAreas = _targetNodes.GetChildrenByType<Area2D>();
        foreach (var tileArea in targetTileAreas)
        {
            var polygons = tileArea.GetChildrenByType<Polygon2D>();
            totalCaptured += polygons.Where(p => p.Color == _capturedColor).ToList().Count;
        }
        var finalGridCoveragePercentage = (double)totalCaptured / (_grid.Width * _grid.Height);
        _scoreController.SetFinalTileCapturedPercentage(finalGridCoveragePercentage);

        var scoreSheet = _scoreController.GetCurrentScoreSheet();
        var enemyStunPoints = _scoreController.CalculateEnemyStunBonusPoints();
        var normalTilePoints = _scoreController.CalculateNormalTilePoints();
        var rushTilePoints = _scoreController.CalculateRushTilePoints();
        var goldTilePoints = _scoreController.CalculateGoldTilePoints();
        var powerTilePoints = _scoreController.CalculatePowerTileBonusPoints();
        var finalGridCoverageBonusPoints = _scoreController.CalculateFinalGridCoverageBonusPoints();

        InsideScreen!.ScoreCounter.Update(_scoreController.CalculateTotalPoints());
        InsideScreen!.Message.Text =
            $"Normal Tiles Captured:  {scoreSheet.TotalNormalTilesCaptured} tiles (+{normalTilePoints})\n" +
            $"Rush Tiles Captured:  {scoreSheet.TotalRushTilesCaptured} tiles (+{rushTilePoints})\n" +
            $"Gold Tiles Captured:  {scoreSheet.TotalGoldTilesCaptured} tiles (+{goldTilePoints})\n" +
            $"Power Tiles Captured:  {scoreSheet.TotalPowerTilesCaptured} tiles (+{powerTilePoints})\n" +
            $"Enemies Stunned:  {scoreSheet.TotalEnemiesStunned} enemies (+{enemyStunPoints})\n" +
            $"Final Grid Coverage:  {(int)(scoreSheet.FinalTilesCapturedPercentage * 100)}% (+{finalGridCoverageBonusPoints})";
    }
}
