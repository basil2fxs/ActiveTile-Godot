using System;
using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.Games.Domination.Config;
using MysticClue.Chroma.GodotClient.Games.Domination.Controllers;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games.Domination;

public static class GridDominationCoopGameVariants
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        var maxPlayers = int.Min(maxSupportedPlayers, 7);
        for (int playerCount = 1; playerCount <= maxPlayers; playerCount++)
        {
            foreach (var difficulty in Enum.GetValues<GameSelection.GameDifficulty>())
            {
                var config = difficulty switch
                {
                    GameSelection.GameDifficulty.Newbie => GetNewbieDifficultyConfig(),
                    GameSelection.GameDifficulty.Regular => GetRegularDifficultyConfig(),
                    GameSelection.GameDifficulty.Elite => GetEliteDifficultyConfig(),
                    _ => throw new InvalidOperationException($"Invalid value for {typeof(GameSelection.GameDifficulty)}: {difficulty}"),
                };
                var playerCountCapture = playerCount;
                yield return new(
                    playerCountCapture,
                    GameSelection.GameType.Cooperative,
                    difficulty,
                    "Domination",
                    null,
                    "Earn points by capturing tiles faster than enemies can eat them. Step on enemies when they're pink to stun them.",
                    GD.Load<Texture2D>("res://HowToPlay/DominationCoop.png"),
                    randomSeed =>
                    {

                        var gridHelper = new GridDominationGridHelper(randomSeed);
                        var gameController = new GridDominationEnemyController(config, gridHelper);
                        var scoreController = new GridDominationScoreController(config.ScoreSystemConfig);
                        return new GridDominationCoopGame(gridSpecs, config, gameController, gridHelper, scoreController, 4f, playerCountCapture, randomSeed);
                    });
            }
        }
    }

    private static GridDominationConfig.GameCoopConfig GetNewbieDifficultyConfig() => new()
    {
        GameDurationSeconds = 60,
        RushModeEnableRemainingSeconds = 20,
        EnemyCountPerPlayer = 1,
        InitialCapturedTilesPercentage = 0.05f,
        CaptureSplashRadius = 2,
        EnemyConfig = new GridDominationConfig.EnemyConfig
        {
            TypeName = "SingleTileEnemy_1",
            Width = 1,
            Height = 1,
            VulnerableEnableDelaySeconds = 15,
            VulnerableDurationSeconds = 5,
            VulnerableIntervalSeconds = 15,
            MovementIntervalSeconds = 0.25,
        },
        ScoreSystemConfig = new GridDominationConfig.ScoreSystemConfig
        {
            PointsPerNormalTile = 1,
            PointsPerPowerTile = 2,
            PointsPerGoldTile = 50,
            PointsPerRushTile = 2,
            PointsPerEnemyStunned = 3,
        },
        GoldTileConfig = new GridDominationConfig.SpecialTileConfig
        {
            SpawnEnableDelaySeconds = 10,
            SpawnDurationSeconds = 5,
            SpawnIntervalSeconds = 10,
            SpawnCount = 5,
        },
        PowerTileConfig = new GridDominationConfig.SpecialTileConfig
        {
            SpawnEnableDelaySeconds = 10,
            SpawnDurationSeconds = 5,
            SpawnIntervalSeconds = 5,
            SpawnCount = 5,
        },
    };

    private static GridDominationConfig.GameCoopConfig GetRegularDifficultyConfig() => new()
    {
        GameDurationSeconds = 60,
        RushModeEnableRemainingSeconds = 20,
        EnemyCountPerPlayer = 1,
        InitialCapturedTilesPercentage = 0.05f,
        CaptureSplashRadius = 1,
        EnemyConfig = new GridDominationConfig.EnemyConfig
        {
            TypeName = "SingleTileEnemy_1",
            Width = 1,
            Height = 1,
            VulnerableEnableDelaySeconds = 15,
            VulnerableDurationSeconds = 5,
            VulnerableIntervalSeconds = 15,
            MovementIntervalSeconds = 0.15,
        },
        ScoreSystemConfig = new GridDominationConfig.ScoreSystemConfig
        {
            PointsPerNormalTile = 1,
            PointsPerPowerTile = 2,
            PointsPerGoldTile = 50,
            PointsPerRushTile = 2,
            PointsPerEnemyStunned = 3,
        },
        GoldTileConfig = new GridDominationConfig.SpecialTileConfig
        {
            SpawnEnableDelaySeconds = 10,
            SpawnDurationSeconds = 5,
            SpawnIntervalSeconds = 10,
            SpawnCount = 5,
        },
        PowerTileConfig = new GridDominationConfig.SpecialTileConfig
        {
            SpawnEnableDelaySeconds = 10,
            SpawnDurationSeconds = 3,
            SpawnIntervalSeconds = 5,
            SpawnCount = 5,
        },
    };

    private static GridDominationConfig.GameCoopConfig GetEliteDifficultyConfig() => new()
    {
        GameDurationSeconds = 60,
        RushModeEnableRemainingSeconds = 20,
        EnemyCountPerPlayer = 2,
        InitialCapturedTilesPercentage = 0.05f,
        CaptureSplashRadius = 1,
        EnemyConfig = new GridDominationConfig.EnemyConfig
        {
            TypeName = "SingleTileEnemy_1",
            Width = 1,
            Height = 1,
            VulnerableEnableDelaySeconds = 15,
            VulnerableDurationSeconds = 5,
            VulnerableIntervalSeconds = 15,
            MovementIntervalSeconds = 0.1,
        },
        ScoreSystemConfig = new GridDominationConfig.ScoreSystemConfig
        {
            PointsPerNormalTile = 1,
            PointsPerPowerTile = 2,
            PointsPerGoldTile = 50,
            PointsPerRushTile = 2,
            PointsPerEnemyStunned = 3,
        },
        GoldTileConfig = new GridDominationConfig.SpecialTileConfig
        {
            SpawnEnableDelaySeconds = 10,
            SpawnDurationSeconds = 5,
            SpawnIntervalSeconds = 10,
            SpawnCount = 5,
        },
        PowerTileConfig = new GridDominationConfig.SpecialTileConfig
        {
            SpawnEnableDelaySeconds = 10,
            SpawnDurationSeconds = 2,
            SpawnIntervalSeconds = 5,
            SpawnCount = 2,
        },
    };
}
