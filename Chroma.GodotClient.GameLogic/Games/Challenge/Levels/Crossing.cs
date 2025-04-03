using System.Drawing;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

public class Crossing : IChallengeGameLevel
{
    public string Name => "Crossing";
    public GameSpecs GetGameSpecs(ResolvedGridSpecs grid)
    {
        PrintedList<GameSpecs.StaticBlock> safe = [];

        // Outer Safes
        const int outerSafeWidth = 2;
        var outerSafeSize = new Size(outerSafeWidth, grid.Height);
        safe.Add(new GameSpecs.StaticBlock(outerSafeSize, new Point(0, 0)));
        safe.Add(new GameSpecs.StaticBlock(outerSafeSize, new Point(grid.Width - outerSafeWidth, 0)));
        // Inner Safes
        var innerSafeSize = new Size(1, 1);
        safe.Add(new GameSpecs.StaticBlock(innerSafeSize, new Point(outerSafeWidth + 2, 2)));
        safe.Add(new GameSpecs.StaticBlock(innerSafeSize, new Point(grid.Width - outerSafeWidth - 3, 2)));
        safe.Add(new GameSpecs.StaticBlock(innerSafeSize, new Point(outerSafeWidth + 2, grid.Height - 3)));
        safe.Add(new GameSpecs.StaticBlock(innerSafeSize, new Point(grid.Width - outerSafeWidth - 3, grid.Height - 3)));
        // Inner Centre Safe
        var widthBetweenOuterSafes = grid.Width - (2 * outerSafeWidth);
        var xCentreSide = widthBetweenOuterSafes % 2 == 0 ? 2 : 1;
        var yCentreSide = grid.Height % 2 == 0 ? 2 : 1;
        var anchor = new Point(
            x: xCentreSide % 2 == 0 ? grid.Width / 2 - 1 : grid.Width / 2,
            y: yCentreSide % 2 == 0 ? grid.Height / 2 - 1 : grid.Height / 2);
        safe.Add(new GameSpecs.StaticBlock(new Size(xCentreSide,yCentreSide), anchor));

        const int dangerWidth = 1;
        const int dangerHeight = 4;
        var startXPos = outerSafeWidth;
        var endXPos = grid.Width - outerSafeWidth - 1;

        var startingPointIntervals = 4;
        var intervalIndex = 1;
        PrintedList<GameSpecs.MovingBlock> danger = [];
        for (var x = startXPos; x <= endXPos; x++)
        {
            if (intervalIndex > startingPointIntervals) intervalIndex = 1;

            var startY = 0 - (intervalIndex * dangerHeight);
            var endY = grid.Height + (intervalIndex * dangerHeight);
            danger.Add(new GameSpecs.MovingBlock(
                new Size(dangerWidth, dangerHeight),
                [new Point(x, startY), new Point(x, endY)],
                GameSpecs.RouteReturn.TeleportToStart
            ));

            intervalIndex++;
        }

        return new GameSpecs(grid.Width, grid.Height, safe, danger);
    }

    public float ProportionTargets => 0.25f;
}
