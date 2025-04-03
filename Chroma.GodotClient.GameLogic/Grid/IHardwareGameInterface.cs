namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Interface on the hardware side to talk to game on the network.
/// </summary>
public interface IHardwareGameInterface : IAsyncDisposable, IDisposable
{
    public delegate void UpdatePixel(int x, int y, byte r, byte g, byte b);
    public void UpdateGrid(ResolvedGridSpecs gridSpecs);

    public Task PutSensorData(ISensorView sv);
}
