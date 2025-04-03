namespace MysticClue.Chroma.GodotClient.Games.Domination.Config;

public static class GridDominationConfig
{
    public record GameCoopConfig
    {
        public required int GameDurationSeconds { get; init; }
        public required int RushModeEnableRemainingSeconds { get; init; }
        public required int EnemyCountPerPlayer { get; init; }
        public required float InitialCapturedTilesPercentage { get; init; }
        public required int CaptureSplashRadius { get; init; }
        public required EnemyConfig EnemyConfig { get; init; }
        public required ScoreSystemConfig ScoreSystemConfig { get; init; }
        public required SpecialTileConfig GoldTileConfig { get; init; }
        public required SpecialTileConfig PowerTileConfig { get; init; }
    }

    public record GamePvpConfig
    {
        public required int GameDurationSeconds { get; init; }
        public required int RushModeEnableRemainingSeconds { get; init; }
        public required PlayerTargetTileConfig PlayerTargetTileConfig { get; init; }
    }

    public record ScoreSystemConfig
    {
        public int PointsPerNormalTile { get; init; }
        public int PointsPerEnemyStunned { get; init; }
        public int PointsPerGoldTile { get; init; }
        public int PointsPerRushTile { get; init; }
        public int PointsPerPowerTile { get; init; }
    }

    public record EnemyConfig
    {
        public required string TypeName { get; init; }
        public required int Height { get; init; }
        public required int Width { get; init; }
        public required int VulnerableEnableDelaySeconds { get; init; }
        public required int VulnerableDurationSeconds { get; init; }
        public required int VulnerableIntervalSeconds { get; init; }
        public required double MovementIntervalSeconds { get; init; }
    }

    public record SpecialTileConfig
    {
        public required int SpawnEnableDelaySeconds { get; init; }
        public required int SpawnDurationSeconds { get; init; }
        public required int SpawnIntervalSeconds { get; init; }
        public required int SpawnCount { get; init; }
    }

    public record PlayerTargetTileConfig
    {
        public required int SpawnDurationSeconds { get; init; }
        public required int SpawnIntervalSeconds { get; init; }
        public required int SpawnCount { get; init; }
        public required int CaptureRadius { get; init; }
    }
}
