namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

public interface IFrameView
{
    /// <summary>
    /// Extracts RGB values for a pixel in grid output coordinates.
    /// </summary>
    public (byte, byte, byte) GetPixel(int x, int y);
}
