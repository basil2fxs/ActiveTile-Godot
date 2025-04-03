using MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;
using System.Threading.Channels;

namespace MysticClue.Chroma.GodotClient.GameLogicTests.Grid.Serial;

/// <summary>
/// Connects two uses of the same port in loopback.
/// </summary>
public class LoopBackSerialPortFactory : SerialPortFactory
{
    public Dictionary<string, ChannelSerialPortStream> PortMap { get; private set; }
    private Dictionary<string, bool> _bothConnected = [];

    public LoopBackSerialPortFactory()
    {
        PortMap = [];
    }

    public override ChannelSerialPortStream MakeSerialPort(string port)
    {
        if (PortMap.TryGetValue(port, out var existing))
        {
            if (_bothConnected.GetValueOrDefault(port, false))
                throw new InvalidOperationException($"Trying to use {port} for the third time.");

            _bothConnected[port] = true;
            return new ChannelSerialPortStream() { In = existing.Out, Out = existing.In };
        }
        else
        {
            var sp = new ChannelSerialPortStream
            {
                In = Channel.CreateBounded<byte>(4096),
                Out = Channel.CreateBounded<byte>(4096)
            };
            PortMap[port] = sp;
            return sp;
        }
    }
}
