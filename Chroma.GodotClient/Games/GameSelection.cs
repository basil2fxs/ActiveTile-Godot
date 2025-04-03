using Godot;
using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System;
using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Games;

/// <summary>
/// Defines a specific variant of a game.
/// </summary>
/// <param name="PlayerCount">The max players supported. For GameType.Competitive games, this is the exact number of players.</param>
/// <param name="Name">A player-visible string for this game variant.</param>
/// <param name="Level">Where a game variant has multiple levels, a player-visible string for this level.</param>
/// <param name="Instantiate">A function that takes a random seed and instantiates the game.</param>
public record struct GameSelection(
    int PlayerCount,
    GameSelection.GameType Type,
    GameSelection.GameDifficulty Difficulty,
    string Name,
    string? Level,
    string HowToPlayText,
    Texture2D HowToPlayImage,
    Func<int, GridGame> Instantiate)
{
    public enum GameType { Cooperative, Competitive, Zen }

    public enum GameDifficulty { Newbie, Regular, Elite }

    public delegate IEnumerable<GameSelection> GetGameVariants(int maxSupportedPlayers, ResolvedGridSpecs gridSpecs);
}
