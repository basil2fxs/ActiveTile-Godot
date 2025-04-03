using Godot;

namespace MysticClue.Chroma.GodotClient;

/// <summary>
/// Child node to store the player associated with the parent node.
/// </summary>
public partial class PlayerIndex : Node
{
    [Export]
    public int Player { get; set; }

    public PlayerIndex(int player) { Player = player; }

    /// <summary>
    /// Gets the Player of the first PlayerIndex child.
    /// </summary>
    /// <param name="node">Node to search.</param>
    /// <returns>Player value or -1 if no PlayerIndex node is found.</returns>
    public static int Of(Node node)
    {
        var p = node.GetChildByType<PlayerIndex>(false);
        return p != null ? p.Player : -1;
    }
}
