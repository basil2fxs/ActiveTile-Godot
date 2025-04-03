using System.Drawing;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge;

/// <summary>
/// Describes a challenge game level.
/// </summary>
public record class GameSpecs
{
    public int GridWidth { get; init; }
    public int GridHeight { get; init; }
    public PrintedList<StaticBlock> Safe { get; init; }
    public PrintedList<MovingBlock> Danger { get; init; }
    public bool[,] PotentialTargets { get; init; }

    public GameSpecs(int GridWidth, int GridHeight, PrintedList<StaticBlock> Safe, PrintedList<MovingBlock> Danger)
    {
        this.GridWidth = GridWidth;
        this.GridHeight = GridHeight;
        this.Safe = Safe;
        this.Danger = Danger;

        PotentialTargets = new bool[GridWidth, GridHeight];
        var game = new GameState(this);
        for (int i = 0; i < 1000; ++i)
        {
            var blocked = game.GetInaccessible();
            for (int x = 0; x < GridWidth; ++x)
            {
                for (int y = 0; y < GridHeight; ++y)
                {
                    if (!blocked[x, y])
                    {
                        PotentialTargets[x, y] = true;
                    }
                }
            }
            game.Step();
        }
    }

    public record struct MovingBlockCurrentPosition(Point Position, int WaypointIndex, BlockMovement NextMovement, BlockMovement LastMovement);

    public enum BlockMovement { Forward, Reversing, Teleporting }

    public record struct MovingBlock
    {
        public Size Size { get; set; }
        public PrintedList<Point> Route { get; set; }
        public Point InitialPosition { get; set; }
        public int InitialWaypoint { get; set; }
        public RouteReturn RouteReturn { get; set; }

        public MovingBlock(Size size, PrintedList<Point> route, RouteReturn routeReturn)
        : this(size, route, route[0], 1, routeReturn) { }

        public MovingBlock(Size size, PrintedList<Point> route, Point initialPosition, int initialWaypoint, RouteReturn routeReturn)
        {
            Size = size;
            Route = route;
            InitialPosition = initialPosition;
            InitialWaypoint = initialWaypoint;
            RouteReturn = routeReturn;
        }
    }

    public record struct StaticBlock(Size Size, Point Position);

    /// <summary>
    /// Mirror a block's position in the grid.
    /// </summary>
    public static int Mirror(int gridSize, int blockSize, int blockPosition) => gridSize - blockSize - blockPosition;

    public enum RouteReturn { ReverseAtEnd, MoveToStart, TeleportToStart }
}
