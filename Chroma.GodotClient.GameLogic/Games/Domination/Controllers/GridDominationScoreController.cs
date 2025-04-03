using MysticClue.Chroma.GodotClient.Games.Domination.Config;
using MysticClue.Chroma.GodotClient.Games.Domination.Models;

namespace MysticClue.Chroma.GodotClient.Games.Domination.Controllers;

public class GridDominationScoreController
{
    private readonly GridDominationConfig.ScoreSystemConfig _scoreSystemConfig;
    private GridDominationScoreSheet _scoreSheet = new();

    public GridDominationScoreController(GridDominationConfig.ScoreSystemConfig scoreSystemConfig)
    {
        _scoreSystemConfig = scoreSystemConfig;
    }

    public void CreateNewScoreSheet()
    {
        _scoreSheet = new();
    }

    public GridDominationScoreSheet GetCurrentScoreSheet()
    {
        return _scoreSheet;
    }

    public int CalculateBasePoints()
    {
        var normalTilePoints = CalculateNormalTilePoints();
        var rushTilePoints = CalculateRushTilePoints();
        var goldTilePoints = CalculateGoldTilePoints();

        var totalBasePoints = normalTilePoints + rushTilePoints + goldTilePoints;
        return totalBasePoints;
    }

    public int CalculateFinalGridCoverageBonusPoints()
    {
        return (int)(CalculateBasePoints() * _scoreSheet.FinalTilesCapturedPercentage);
    }

    public int CalculateTotalBonusPoints()
    {
        var enemyStunPoints = CalculateEnemyStunBonusPoints();
        var powerTilePoints = CalculatePowerTileBonusPoints();
        var finalGridCoveragePoints = CalculateFinalGridCoverageBonusPoints();

        return enemyStunPoints + powerTilePoints + finalGridCoveragePoints;
    }

    public int CalculateTotalPoints()
    {
        var basePoints = CalculateBasePoints();
        var bonusPoints = CalculateTotalBonusPoints();

        return basePoints + bonusPoints;
    }

    public int CalculateNormalTilePoints()
    {
        return _scoreSheet.TotalNormalTilesCaptured * _scoreSystemConfig.PointsPerNormalTile;
    }

    public int CalculateRushTilePoints()
    {
        return _scoreSheet.TotalRushTilesCaptured * _scoreSystemConfig.PointsPerRushTile;
    }

    public int CalculateGoldTilePoints()
    {
        return _scoreSheet.TotalGoldTilesCaptured * _scoreSystemConfig.PointsPerGoldTile;
    }

    public int CalculateEnemyStunBonusPoints()
    {
        return _scoreSheet.TotalEnemiesStunned * _scoreSystemConfig.PointsPerEnemyStunned;
    }

    public int CalculatePowerTileBonusPoints()
    {
        return _scoreSheet.TotalPowerTilesCaptured * _scoreSystemConfig.PointsPerPowerTile;
    }

    public void AddPowerTileCount(int? total = 1)
    {
        total ??= 1;
        _scoreSheet.TotalPowerTilesCaptured += total.Value;
    }

    public void AddNormalTileCount(int? total = 1)
    {
        total ??= 1;
        _scoreSheet.TotalNormalTilesCaptured += total.Value;
    }

    public void AddRushTileCount(int? total = 1)
    {
        total ??= 1;
        _scoreSheet.TotalRushTilesCaptured += total.Value;
    }

    public void AddGoldTileCount(int? total = 1)
    {
        total ??= 1;
        _scoreSheet.TotalGoldTilesCaptured += total.Value;
    }

    public void AddEnemyStunCount(int? total = 1)
    {
        total ??= 1;
        _scoreSheet.TotalEnemiesStunned += total.Value;
    }

    public void SetFinalTileCapturedPercentage(double percentage)
    {
        _scoreSheet.FinalTilesCapturedPercentage = percentage;
    }
}
