using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

public class Gauntlet : IChallengeGameLevel
{
    public string Name => "Gauntlet";

    public GameSpecs GetGameSpecs(ResolvedGridSpecs grid)
    {
        const int SafeWidth = 2;
        PrintedList<GameSpecs.StaticBlock> safe = [
            new(new(SafeWidth, grid.Height), new(0, 0)),
            new(new(SafeWidth, grid.Height), new(grid.Width - SafeWidth, 0)),
        ];

        // Prefer dangers to be size 3, but can be smaller to fit grid.
        int dangerWidth = 3;
        PrintedList<GameSpecs.MovingBlock> danger = [];
        var addDangers = (int xPos, int size, bool withMirror = true) =>
        {
            int yEnd = grid.Height - dangerWidth;
            danger.Add(new(new(size, size), [new(xPos, 0), new(xPos, yEnd)], GameSpecs.RouteReturn.MoveToStart));
            if (withMirror)
            {
                int xPos2 = GameSpecs.Mirror(grid.Width, size, xPos);
                danger.Add(new(new(size, size), [new(xPos2, 0), new(xPos2, yEnd)], GameSpecs.RouteReturn.MoveToStart));
            }
        };
        int xPos = SafeWidth;
        while (grid.Width - 2 * xPos >= 2 * dangerWidth)
        {
            addDangers(xPos, dangerWidth);
            xPos += dangerWidth;
        }
        {
            int remainder = grid.Width - 2 * xPos;
            switch (remainder)
            {
                case 5:
                case 4:
                    addDangers(xPos, 2);
                    break;
                case 3:
                case 2:
                    addDangers(xPos, remainder, false);
                    break;
            }
        }

        // Sort by X position so we can flip the route of alternating blocks.
        danger.Sort((d1, d2) => d1.Route[0].X - d2.Route[0].X);
        for (int i = 0; i < danger.Count; ++i)
        {
            var route = danger[i].Route;
            var size = danger[i].Size.Height;
            if (i % 2 == 0)
            {
                route[0] = route[0] with { Y = GameSpecs.Mirror(grid.Height, size, route[0].Y) };
                route[1] = route[1] with { Y = GameSpecs.Mirror(grid.Height, size, route[1].Y) };
            }
            danger[i] = danger[i] with { InitialPosition = route[0] };
        }

        return new(grid.Width, grid.Height, safe, danger);
    }

    public float ProportionTargets => 0.1f;
}
