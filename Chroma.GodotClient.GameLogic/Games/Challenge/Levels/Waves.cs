using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

public class Waves : IChallengeGameLevel
{
    public string Name => "Waves";

    public GameSpecs GetGameSpecs(ResolvedGridSpecs grid)
    {
        const int SafeHeight = 2;
        PrintedList<GameSpecs.StaticBlock> safe = [
            new(new(grid.Width, SafeHeight), new(0, grid.Height - SafeHeight)),
        ];

        const int SpaceBetween = 4;
        int dangerCount = (int)float.Ceiling((grid.Height - SafeHeight + 1) / (float)SpaceBetween);
        int totalHeight = dangerCount * SpaceBetween;
        int finalYPos = grid.Height - SafeHeight - 1;
        int initialYPos = finalYPos - totalHeight + 1;
        PrintedList<GameSpecs.MovingBlock> danger = [];
        for (int i = 0; i < dangerCount; ++i)
        {
            int yPos = initialYPos + i * SpaceBetween;
            danger.Add(new(new(grid.Width, 1), [new(0, initialYPos), new(0, finalYPos)], new(0, yPos), 1, GameSpecs.RouteReturn.TeleportToStart));
        }

        return new(grid.Width, grid.Height, safe, danger);
    }

    public float ProportionTargets => 0.1f;
}
