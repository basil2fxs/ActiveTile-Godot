using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using MysticClue.Chroma.GodotClient.Games.Domination.Config;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games.Domination;

public static class GridDominationPvpGameVariants
{
    public static IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs)
    {
        var gameConfig = new GridDominationConfig.GamePvpConfig
        {
            GameDurationSeconds = 120,
            RushModeEnableRemainingSeconds = 30,
            PlayerTargetTileConfig = new GridDominationConfig.PlayerTargetTileConfig
            {
                SpawnDurationSeconds = 5,
                SpawnIntervalSeconds = 2,
                SpawnCount = 4, // TODO - Make this dependent on the grid size later maybe?
                CaptureRadius = 1
            }
        };

        int maxPlayers = int.Min(maxSupportedPlayers, GridDominationPvpGame.MaxPlayerCount(gridSpecs, gameConfig));
        for (int playerCount = 2; playerCount <= maxPlayers; playerCount++)
        {
            int playerCountCapture = playerCount;
            yield return new(
                playerCount,
                GameSelection.GameType.Competitive,
                GameSelection.GameDifficulty.Regular,
                "DominationPvp",
                null,
                "Claim tiles by stepping on your color. Most tiles at the end wins.",
                GD.Load<Texture2D>("res://HowToPlay/DominationPvp.png"),
                randomSeed =>
                {
                    var gridHelper = new GridDominationGridHelper(randomSeed);
                    return new GridDominationPvpGame(gridSpecs, gameConfig, gridHelper, playerCountCapture, randomSeed);
                });
        }
    }
}
