using MysticClue.Chroma.GodotClient.GameLogic.Physics;
using MysticClue.Chroma.GodotClient.Games.Domination.Config;
using MysticClue.Chroma.GodotClient.Games.Domination.Models;
using Size = System.Drawing.Size;

namespace MysticClue.Chroma.GodotClient.Games.Domination.Controllers;

public class GridDominationEnemyController
{
    private static readonly Random Rand = new();
    private readonly GridDominationConfig.GameCoopConfig _gameCoopConfig;
    private readonly Dictionary<string, GridDominationEnemy> _enemies = [];
    private readonly Dictionary<string, Grid2DRoute> _enemyRoutes = [];

    public GridDominationEnemyController(GridDominationConfig.GameCoopConfig gameCoopConfig, GridDominationGridHelper gridHelper)
    {
        _gameCoopConfig = gameCoopConfig;
    }

    /// <summary>
    /// Get a read-only list of enemies
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<GridDominationEnemy> GetEnemiesReadonly()
    {
        return _enemies.Values.ToList();
    }

    /// <summary>
    /// Get the list of enemies without any restrictions
    /// </summary>
    /// <returns></returns>
    public List<GridDominationEnemy> GetEnemies()
    {
        return _enemies.Values.ToList();
    }

    /// <summary>
    /// Checks if an enemy is done moving based on its remaining movements in its current route
    /// </summary>
    /// <param name="enemyName">Enemy identifier</param>
    /// <returns>Flag indicating if enemy is done moving. Defaults to false if enemy does not exist.</returns>
    public bool IsEnemyRouteComplete(string enemyName)
    {
        var enemyRouteFound = _enemyRoutes.TryGetValue(enemyName, out var route);
        if (!enemyRouteFound) return true;

        if (route!.GetRemainingMovementCount() == 0) return true;

        var currentMovement = route.GetCurrentMovement();
        if (currentMovement == null) return true;
        if (currentMovement.RemainingSteps == 0) return true;
        return false;
    }

    /// <summary>
    /// Get the current direction of an enemy
    /// </summary>
    /// <param name="enemyName">Enemy identifier</param>
    /// <returns>Current direction of enemy. Null if enemy does not exist.</returns>
    public Grid2DMovement? GetEnemyCurrentMovement(string enemyName)
    {
        var enemyRouteFound = _enemyRoutes.TryGetValue(enemyName, out var route);
        if (!enemyRouteFound) return null;

        var currentMovement = route!.GetCurrentMovement();
        if (currentMovement == null) return null;

        return currentMovement;
    }

    /// <summary>
    /// Decrement the remaining steps of an enemy by specified total steps. If remaining steps is already zero, do nothing.
    /// </summary>
    /// <param name="enemyName">Enemy identifier</param>
    /// <param name="totalSteps">Total steps to take</param>
    public void DecrementCurrentMovementSteps(string enemyName, int? totalSteps = 1)
    {
        totalSteps ??= 1;
        if (totalSteps == 0) return;

        var enemyRouteFound = _enemyRoutes.TryGetValue(enemyName, out var route);
        if (!enemyRouteFound) return;

        var currentMovement = route!.GetCurrentMovement();
        if (currentMovement == null) return;

        if (currentMovement.RemainingSteps == 0) return;
        currentMovement.RemainingSteps = Math.Max(0, currentMovement.RemainingSteps - totalSteps.Value);
    }

    public void TryRemoveCurrentMovementIfCompleted(string enemyName)
    {
        var enemyRouteFound = _enemyRoutes.TryGetValue(enemyName, out var route);
        if (!enemyRouteFound) return;

        var currentMovement = route!.GetCurrentMovement();
        if (currentMovement == null) return;

        if(currentMovement.RemainingSteps == 0) route.CompleteCurrentMovement();
    }

    /// <summary>
    /// Set a new movement action for an enemy.
    /// </summary>
    /// <param name="enemyName">Enemy identifier</param>
    /// <param name="movements">List of movements to traverse, each consisting of total steps and direction</param>
    public void SetEnemyRoute(string enemyName, List<Grid2DMovement> movements)
    {
        if (!_enemyRoutes.TryGetValue(enemyName, out var route))
            return;

        route.SetRoute(movements);
    }

    /// <summary>
    /// Initialize enemies based on the game config.
    /// This calls the GetRandomizedEnemiesFromPool to get a set of randomized enemies based on the config.
    /// Each randomized enemies are then provided their initial movement actions
    /// </summary>
    /// <returns>A read-only list of enemies</returns>
    public IReadOnlyList<GridDominationEnemy> InitializeEnemiesAsReadonly(int totalPlayers)
    {
        var enemies = CreateEnemies(totalPlayers);
        foreach (var enemy in enemies)
        {
            _enemies.TryAdd(enemy.Name, enemy);
            _enemyRoutes.TryAdd(enemy.Name, new Grid2DRoute());
        }

        return _enemies.Values.ToList();
    }

    /// <summary>
    /// Get a randomized list of enemies from an enemy pool defined by the GameConfig
    /// </summary>
    /// <returns>A list of randomized enemies</returns>
    private List<GridDominationEnemy> CreateEnemies(int totalPlayers)
    {
        var totalEnemiesToCreate = _gameCoopConfig.EnemyCountPerPlayer * totalPlayers;

        var enemies = new List<GridDominationEnemy>();
        while (enemies.Count < totalEnemiesToCreate)
        {
            var newEnemy = new GridDominationEnemy
            {
                Name = $"{_gameCoopConfig.EnemyConfig.TypeName}_{enemies.Count + 1}",
                Size = new Size(_gameCoopConfig.EnemyConfig.Width, _gameCoopConfig.EnemyConfig.Height)
            };
            enemies.Add(newEnemy);
        }

        return enemies;
    }
}
