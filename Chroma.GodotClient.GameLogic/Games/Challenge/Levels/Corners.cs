using System.Drawing;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

public class Corners : IChallengeGameLevel
{
    public string Name => "Corners";

    public GameSpecs GetGameSpecs(ResolvedGridSpecs grid)
    {
        PrintedList<GameSpecs.StaticBlock> safe = [];
        var safeCornerSize = new Size(2, 2);
        safe.Add(new GameSpecs.StaticBlock(safeCornerSize, new Point(0, 0)));
        safe.Add(new GameSpecs.StaticBlock(safeCornerSize, new Point(0, grid.Height - safeCornerSize.Height)));
        safe.Add(new GameSpecs.StaticBlock(safeCornerSize, new Point(grid.Width - safeCornerSize.Width, 0)));
        safe.Add(new GameSpecs.StaticBlock(safeCornerSize, new Point(grid.Width - safeCornerSize.Width, grid.Height - safeCornerSize.Height)));

        PrintedList<GameSpecs.MovingBlock> danger = [];
        var outerDangerSize = new Size(2, 2);
        // Left Danger
        danger.Add(new GameSpecs.MovingBlock(
            outerDangerSize,
            [
                new Point(0, grid.Height - outerDangerSize.Height - outerDangerSize.Height),
                new Point(0, outerDangerSize.Height)
            ],
            GameSpecs.RouteReturn.ReverseAtEnd
        ));
        // Top Danger
        danger.Add(new GameSpecs.MovingBlock(
            outerDangerSize,
            [
                new Point(grid.Width - outerDangerSize.Width - safeCornerSize.Width, 0),
                new Point(outerDangerSize.Width, 0)
            ],
            GameSpecs.RouteReturn.ReverseAtEnd
        ));
        // Right Danger
        danger.Add(new GameSpecs.MovingBlock(
            outerDangerSize,
            [
                new Point(grid.Width - outerDangerSize.Width, outerDangerSize.Height),
                new Point(grid.Width - outerDangerSize.Width, grid.Height - outerDangerSize.Height - safeCornerSize.Height)
            ],
            GameSpecs.RouteReturn.ReverseAtEnd
        ));
        // Bottom Danger
        danger.Add(new GameSpecs.MovingBlock(
            outerDangerSize,
            [
                new Point(outerDangerSize.Width, grid.Height - outerDangerSize.Height),
                new Point(grid.Width - outerDangerSize.Width - safeCornerSize.Width, grid.Height - outerDangerSize.Height),
            ],
            GameSpecs.RouteReturn.ReverseAtEnd
        ));
        // Center Dangers
        var centerDangerSize = new Size(1, 1);
        for (var y = safeCornerSize.Height; y < grid.Height - safeCornerSize.Height; y++)
        {
            var startX = y % 2 == 0 ? safeCornerSize.Width : grid.Width - safeCornerSize.Width - 1;
            var endX = y % 2 == 0 ? grid.Width - safeCornerSize.Width - 1: safeCornerSize.Width;
            danger.Add(new GameSpecs.MovingBlock(
                centerDangerSize,
                [new Point(startX, y), new Point(endX, y)],
                GameSpecs.RouteReturn.ReverseAtEnd
            ));
        }
        return new GameSpecs(grid.Width, grid.Height, safe, danger);
    }

    public float ProportionTargets => 0.25f;
}
