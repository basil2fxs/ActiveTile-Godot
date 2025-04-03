using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using Point = System.Drawing.Point;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

public class Blender : IChallengeGameLevel
{
    public string Name => "Blender";

    public GameSpecs GetGameSpecs(ResolvedGridSpecs grid)
    {
        const int MinSafeSize = 3;
        int dangerSize = (int)Math.Floor(Math.Min((grid.Width - MinSafeSize) / 2.0, (grid.Height - MinSafeSize) / 2.0));
        int dangerInner = (int)Math.Floor(dangerSize / 2.0);
        int dangerOuter = (int)Math.Ceiling(dangerSize / 2.0);

        PrintedList<GameSpecs.StaticBlock> safe = [
            new(
                new(grid.Width - 2 * dangerSize, grid.Height - 2 * dangerSize),
                new(dangerSize, dangerSize)
            ),
        ];

        PrintedList<GameSpecs.MovingBlock> danger = [];
        Point outerOpposite = new(grid.Width - dangerOuter, grid.Height - dangerOuter);
        PrintedList<Point> outerRoute = [
            new(0, 0), new(outerOpposite.X, 0), outerOpposite, new(0, outerOpposite.Y),
        ];
        danger.Add(new(new(dangerOuter, dangerOuter), outerRoute, new(0, 0), 1, GameSpecs.RouteReturn.MoveToStart));
        danger.Add(new(new(dangerOuter, dangerOuter), outerRoute, outerOpposite, 3, GameSpecs.RouteReturn.MoveToStart));

        Point innerInitial = new(dangerOuter, dangerOuter);
        Point innerOpposite = new(grid.Width - dangerSize, grid.Height - dangerSize);
        PrintedList<Point> innerRoute = [
            innerInitial,
            new(innerInitial.X, innerOpposite.Y),
            innerOpposite,
            new(innerOpposite.X, innerInitial.Y),
        ];
        danger.Add(new(new(dangerInner, dangerInner), innerRoute, innerInitial, 1, GameSpecs.RouteReturn.MoveToStart));
        danger.Add(new(new(dangerInner, dangerInner), innerRoute, innerOpposite, 3, GameSpecs.RouteReturn.MoveToStart));


        return new(grid.Width, grid.Height, safe, danger);
    }

    public float ProportionTargets => 0.1f;
}
