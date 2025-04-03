namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

public interface ISensorView
{
    /// <summary>
    /// Extracts sensor values for a tile in grid output coordinates.
    /// </summary>
    public byte GetTile(int x, int y);
}
