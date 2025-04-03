using System.Drawing;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

public class Bridge : IChallengeGameLevel
{
    public string Name => "Bridge";

    public GameSpecs GetGameSpecs(ResolvedGridSpecs grid)
    {
        PrintedList<GameSpecs.StaticBlock> safe = [];

        var safeBridgeWidth = grid.Width % 2 == 0 ? 2 : 3;
        var safeBridgeSize = new Size(safeBridgeWidth, grid.Height);
        var xAnchor = grid.Width / 2 - 1;
        safe.Add(new GameSpecs.StaticBlock(safeBridgeSize, new Point(xAnchor, 0)));

        var dangerSize = new Size(1, 1);
        PrintedList<GameSpecs.MovingBlock> danger = [];
        // Left side of bridge
        for (var y = 0; y < grid.Height; y++)
        {
            var startX = y % 2 == 0 ? 0 : xAnchor - 1;
            var endX = y % 2 == 0 ? xAnchor - 1 : 0;
            danger.Add(new GameSpecs.MovingBlock(
                dangerSize,
                [new Point(startX, y), new Point(endX, y)],
                GameSpecs.RouteReturn.ReverseAtEnd));
        }

        // Right side of bridge
        for (var y = 0; y < grid.Height; y++)
        {
            var startX = y % 2 == 0 ? xAnchor + safeBridgeWidth : grid.Width - 1;
            var endX = y % 2 == 0 ? grid.Width - 1 : xAnchor + safeBridgeWidth;
            danger.Add(new GameSpecs.MovingBlock(
                dangerSize,
                [new Point(startX, y), new Point(endX, y)],
                GameSpecs.RouteReturn.ReverseAtEnd));
        }
        return new GameSpecs(grid.Width, grid.Height, safe, danger);
    }

    public float ProportionTargets => 0.25f;
}
