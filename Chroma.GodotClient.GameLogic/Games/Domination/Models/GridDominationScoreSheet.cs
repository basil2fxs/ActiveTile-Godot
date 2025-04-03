namespace MysticClue.Chroma.GodotClient.Games.Domination.Models;

public class GridDominationScoreSheet
{
    public int TotalNormalTilesCaptured { get; set; }
    public int TotalRushTilesCaptured { get; set; }
    public int TotalEnemiesStunned { get; set; }
    public int TotalGoldTilesCaptured { get; set; }
    public int TotalPowerTilesCaptured { get; set; }
    public double FinalTilesCapturedPercentage { get; set; }
}
