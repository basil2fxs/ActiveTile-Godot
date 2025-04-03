using MysticClue.Chroma.GodotClient.GameLogic.Physics;
using Size = System.Drawing.Size;

namespace MysticClue.Chroma.GodotClient.Games.Domination.Models;

/// <summary>
/// An enemy is defined by a name and its block size
/// </summary>
public class GridDominationEnemy
{
    public required string Name { get; set; }
    public required Size Size { get; set; }
    public Grid2DMovementDirection? LastDirection { get; set; }
}
