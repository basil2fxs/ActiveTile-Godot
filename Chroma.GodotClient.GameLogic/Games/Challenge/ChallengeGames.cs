using MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge.Levels;

namespace MysticClue.Chroma.GodotClient.GameLogic.Games.Challenge;

public static class ChallengeGames
{
    public static IReadOnlyList<IChallengeGameLevel> Levels => _levels;

    private static IChallengeGameLevel[] _levels = [
        new Gauntlet(),
        new Waves(),
        new Blender(),
        new Crossing(),
        new Corners(),
        new Swirl(),
        new Bridge(),
    ];
}
