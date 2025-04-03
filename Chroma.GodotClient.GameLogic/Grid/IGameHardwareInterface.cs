namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Interface on the game side to talk to hardware.
/// </summary>
public interface IGameHardwareInterface : IAsyncDisposable, IDisposable
{
    public bool AllConnected { get; }
    public Task PutFrame(IFrameView fv);

    public delegate void UpdateSensor(int x, int y, byte sensorData);
    public UpdateSensor? UpdateSensorCallback { get; set; }
}
