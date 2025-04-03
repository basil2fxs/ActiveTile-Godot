namespace MysticClue.Chroma.GodotClient.GameLogic.Physics;

/// <summary>
/// This movement class represents how a block is expected to move and to which direction
/// </summary>
public class Grid2DMovement
{
    public required Grid2DMovementDirection Direction { get; set; }
    public required int RemainingSteps { get; set; }
}