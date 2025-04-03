
using MysticClue.Chroma.GodotClient.GameLogic.Grid;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge;

public interface IChallengeGameLevel
{
    string Name { get; }
    GameSpecs GetGameSpecs(ResolvedGridSpecs grid);
    float ProportionTargets { get; }
}
