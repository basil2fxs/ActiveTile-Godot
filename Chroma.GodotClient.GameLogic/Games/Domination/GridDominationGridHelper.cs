using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.GameLogic.Physics;
using ArgumentException = System.ArgumentException;
using Point = System.Drawing.Point;

namespace MysticClue.Chroma.GodotClient.Games.Domination;

/// <summary>
/// A set of helper functions to facilitate Domination game and its grid
/// </summary>
public class GridDominationGridHelper
{
    private readonly Random _rand;

    public GridDominationGridHelper(int randomSeed)
    {
        _rand = new Random(randomSeed);
    }

    /// <summary>
    /// Checks if a node position is at the edge of the grid based on its current movement direction
    /// </summary>
    /// <param name="grid">Grid specification</param>
    /// <param name="position">Node position to check if it is at its directional limit</param>
    /// <param name="direction">Direction the node is moving towards</param>
    /// <returns>A boolean flag indicating if the grid is at its directional limit</returns>
    public static bool IsNodeAtGridLimitBasedOnDirection(ResolvedGridSpecs grid, Point position, Grid2DMovementDirection direction)
    {
        switch (direction)
        {
            case Grid2DMovementDirection.UP:
                return position.Y == 0;
            case Grid2DMovementDirection.DOWN:
                return position.Y == grid.Height - 1;
            case Grid2DMovementDirection.LEFT:
                return position.X == 0;
            case Grid2DMovementDirection.RIGHT:
                return position.X == grid.Width - 1;
            default:
                return true; // Default to edge of direction
        }
    }

    /// <summary>
    /// Gets a random movement represented by a direction and total steps to take.
    /// </summary>
    /// <param name="grid">Grid specification</param>
    /// <param name="position">Node position to check if it is at its directional limit</param>
    /// <param name="directionToAvoid">Direction to filter out from possible randomized directions</param>
    /// <returns></returns>
    public Grid2DMovement? GetRandomMovement(ResolvedGridSpecs grid, Point position, Grid2DMovementDirection? directionToAvoid = null)
    {
        var randomDirection = GetRandomDirection(directionToAvoid);
        var randomSteps = GetRandomStepCountWithinGrid(grid, position, randomDirection);

        if (randomSteps == 0)
            return null;

        return new Grid2DMovement { Direction = randomDirection, RemainingSteps = randomSteps };
    }

    public static List<Grid2DMovement> GetMovementsToPosition(ResolvedGridSpecs grid, Point fromPosition,
        Point toPosition)
    {
        // Validate destination position is within the grid
        var xOutOfBounds = toPosition.X >= grid.Width || toPosition.X < 0;
        var yOutOfBounds = toPosition.Y >= grid.Height || toPosition.Y < 0;
        if (xOutOfBounds || yOutOfBounds)
        {
            Assert.Report(new ArgumentException("Destination position is out of bounds"));
            return [];
        }

        List<Grid2DMovement> movements = [];

        var horizontalStepCount = toPosition.X - fromPosition.X;
        switch (horizontalStepCount)
        {
            case < 0:
                movements.Add(new Grid2DMovement
                {
                    Direction = Grid2DMovementDirection.LEFT,
                    RemainingSteps = Math.Abs(horizontalStepCount)
                });
                break;
            case > 0:
                movements.Add(new Grid2DMovement
                {
                    Direction = Grid2DMovementDirection.RIGHT,
                    RemainingSteps = Math.Abs(horizontalStepCount)
                });
                break;
        }

        var verticalStepCount = toPosition.Y - fromPosition.Y;
        switch (verticalStepCount)
        {
            case < 0:
                movements.Add(new Grid2DMovement
                {
                    Direction = Grid2DMovementDirection.UP,
                    RemainingSteps = Math.Abs(verticalStepCount)
                });
                break;
            case > 0:
                movements.Add(new Grid2DMovement
                {
                    Direction = Grid2DMovementDirection.DOWN,
                    RemainingSteps = Math.Abs(verticalStepCount)
                });
                break;
        }

        return movements;
    }

    /// <summary>
    /// Gets a random step count that is inclusive of 0 and the max available steps for a node towards its movement direction
    /// </summary>
    /// <param name="grid">Grid specification</param>
    /// <param name="position">Node position to check if it is at its directional limit</param>
    /// <param name="direction">Direction to which the available steps is counted</param>
    /// <returns>A random step count</returns>
    public int GetRandomStepCountWithinGrid(ResolvedGridSpecs grid, Point position, Grid2DMovementDirection direction)
    {
        int availableSteps;
        switch (direction)
        {
            case Grid2DMovementDirection.UP:
                availableSteps = position.Y;
                break;
            case Grid2DMovementDirection.DOWN:
                availableSteps = grid.Height - position.Y - 1;
                break;
            case Grid2DMovementDirection.LEFT:
                availableSteps = position.X ;
                break;
            case Grid2DMovementDirection.RIGHT:
                availableSteps = grid.Width - position.X - 1;
                break;
            default:
                availableSteps = 1;
                break;
        }

        availableSteps = Math.Max(availableSteps, 0);
        if (availableSteps == 0) return 0;

        var randomSteps = _rand.Next(1, availableSteps + 1);
        return randomSteps;
    }

    /// <summary>
    /// Get a randomly selected movement direction that is not the same as the provided current direction (if provided)
    /// </summary>
    /// <param name="directionToAvoid">Direction to filter out from possible randomized directions</param>
    /// <returns>A randomly selected movement direction</returns>
    public Grid2DMovementDirection GetRandomDirection(Grid2DMovementDirection? directionToAvoid = null)
    {
        var directions = Enum.GetValues<Grid2DMovementDirection>().ToList();

        // Avoid assigning the same direction whenever movement stops
        if(directionToAvoid != null) directions.Remove(directionToAvoid.Value);
        return directions[_rand.Next(0, directions.Count)];
    }

    /// <summary>
    /// Get a randomly selected position within the grid
    /// </summary>
    /// <param name="grid">Grid specifications</param>
    /// <returns>A randomly selected position</returns>
    public Point GetRandomPositionWithinGrid(ResolvedGridSpecs grid)
    {
        var x = _rand.Next(0, grid.Width - 1);
        var y = _rand.Next(0, grid.Height - 1);
        return new Point(x, y);
    }

    /// <summary>
    /// Get a randomly selected position within the grid, and excluding specified positions.
    /// This method is O(N^2) at worst case, and should be used with performance in mind.
    /// </summary>
    /// <param name="grid">Grid specifications</param>
    /// <param name="avoidPositions">Positions to avoid in the randomization</param>
    /// <returns>A randomly selected position. If all positions are specified to be avoided, NULL is returned.</returns>
    public Point? TryGetRandomPositionWithinGrid(ResolvedGridSpecs grid, List<Point> avoidPositions)
    {
        List<Point> allowedGridPositions = [];

        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (avoidPositions.Any(p => p.X == x && p.Y == y)) continue;
                allowedGridPositions.Add(new Point(x, y));
            }
        }

        if (allowedGridPositions.Count <= 0) return null;
        return allowedGridPositions.ElementAt(_rand.Next(0, allowedGridPositions.Count - 1));
    }

    public static IEnumerable<Point> GetPositionsByRadius(ResolvedGridSpecs grid, int centreX, int centreY, int radius)
    {
        for (var x = -1 * radius; x <= radius; x++)
        {
            for (var y = -1 * radius; y <= radius; y++)
            {
                var px = centreX + x;
                var py = centreY + y;

                // Ignore centre position
                if (x == 0 && y == 0) continue;
                // Ignore out of bounds
                if (0 > px || px >= grid.Width ) continue;
                if (0 > py || py >= grid.Height ) continue;

                yield return new Point(centreX + x, centreY + y);
            }
        }
    }

    public static IEnumerable<Point> GetPositionsByCrossDirection(ResolvedGridSpecs grid, int centreX, int centreY)
    {
        List<Point> crossPositions = [];

        for (var x = 0; x < grid.Width; x++)
        {
            if (x == centreX) continue;

            var crossPosition = new Point(x, centreY);
            crossPositions.Add(crossPosition);
        }

        for (var y = 0; y < grid.Height; y++)
        {
            if (y == centreY) continue;

            var crossPosition = new Point(centreX, y);
            crossPositions.Add(crossPosition);
        }

        return crossPositions
            .Where(p => 0 <= p.X && p.X < grid.Width)
            .Where(p => 0 <= p.Y && p.Y < grid.Height);
    }

    public static IEnumerable<Point> GetGridOuterLayerPositions(ResolvedGridSpecs grid, int totalLayers)
    {
        // Left Layer
        for (var x = 0; x < totalLayers; x++)
        {
            for (var y = 0; y < grid.Height; y++) yield return new Point(x,y);
        }

        // Right Layer
        for (var x = grid.Width - totalLayers; x < grid.Width; x++)
        {
            for(var y = 0; y < grid.Height; y++) yield return new Point(x,y);
        }

        // Center Top and Bottom Layer
        for (var x = totalLayers; x < grid.Width - totalLayers; x++)
        {
            // Top Layer
            for(var y = 0; y < totalLayers; y++) yield return new Point(x,y);
            // Bottom Layer
            for(var y = grid.Height - 1; y > grid.Height - totalLayers - 1; y--) yield return new Point(x,y);
        }
    }
}
