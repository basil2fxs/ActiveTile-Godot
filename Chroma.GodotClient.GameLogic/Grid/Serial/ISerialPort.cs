namespace MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;

public interface ISerialPort : IDisposable
{
    public Stream Stream { get; }
}
