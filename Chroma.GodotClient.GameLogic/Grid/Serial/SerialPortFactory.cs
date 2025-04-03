namespace MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;

public class SerialPortFactory
{
    public virtual ISerialPort MakeSerialPort(string port) => new SerialPort(port);
}
