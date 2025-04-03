using System.IO.Ports;
using SystemSerialPort = System.IO.Ports.SerialPort;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;

/// <summary>
/// Wrap a real serial port just to implement ISerialPort, which is needed for tests.
/// </summary>
public sealed class SerialPort : ISerialPort, IDisposable
{
    public static string[] GetPortNames() => SystemSerialPort.GetPortNames();

    public SystemSerialPort Actual { get; private set; }

    public SerialPort(string port)
    {
        Actual = new SystemSerialPort(port);
        Actual.BaudRate = 115200;
        Actual.Parity = Parity.None;
        Actual.DataBits = 8;
        Actual.DtrEnable = true;
        Actual.RtsEnable = true;
        Actual.Open();
    }

    public Stream Stream => Actual.BaseStream;

    public void Dispose()
    {
        Actual.Close();
        Actual.Dispose();
    }
}
