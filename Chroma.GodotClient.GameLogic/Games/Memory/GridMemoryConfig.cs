namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Memory;

public static class GridMemoryConfig
{
    public record GameConfig
    {
        public required int TotalRounds { get; init; }
        public required bool StaggeredReveal { get; init; }
        public required bool FasterRevealPerRound { get; init; }
        public required double FasterRevealDurationDecreaseSeconds { get; init; }
        public required double TargetRevealSpawnIntervalSeconds { get; init; }
        public required double TargetRevealRemainDurationSeconds { get; init; }
    }

    public static GameConfig GetNewbiePvpConfig() => new()
        {
            TotalRounds = 10,
            StaggeredReveal = false,
            FasterRevealPerRound = false,
            FasterRevealDurationDecreaseSeconds = 0,
            TargetRevealSpawnIntervalSeconds = 0.5,
            TargetRevealRemainDurationSeconds = 5,
        };

    public static GameConfig GetRegularPvpConfig() => new()
        {
            TotalRounds = 10,
            StaggeredReveal = false,
            FasterRevealPerRound = true,
            FasterRevealDurationDecreaseSeconds = 0.01,
            TargetRevealSpawnIntervalSeconds = 0.3,
            TargetRevealRemainDurationSeconds = 3,
        };

    public static GameConfig GetElitePvpConfig() => new()
        {
            TotalRounds = 10,
            StaggeredReveal = true,
            FasterRevealPerRound = true,
            FasterRevealDurationDecreaseSeconds = 0.02,
            TargetRevealSpawnIntervalSeconds = 0.25,
            TargetRevealRemainDurationSeconds = 0,
        };
}
