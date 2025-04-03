using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Implements connection to the game on a serial port.
/// </summary>
public sealed class SerialHardwareGameInterface : SerialMessageInterface, IHardwareGameInterface
{
    private byte[][] _sensorData;

    protected override ReadOnlySpan<byte> InboundMessageHeader(int chainIndex) => GridSpecs!.RgbHeader;
    protected override int InboundMessageLength(int chainIndex) => GridSpecs!.RgbMessageLength(chainIndex);

    public delegate void UpdatePixel(int x, int y, byte r, byte g, byte b);
    private UpdatePixel _updatePixelCallback;

    public SerialHardwareGameInterface(UpdatePixel updatePixelCallback, SerialPortFactory portFactory) : base(portFactory)
    {
        _sensorData = [];
        _updatePixelCallback = updatePixelCallback;
    }

    public override void UpdateGrid(ResolvedGridSpecs gridSpecs)
    {
        base.UpdateGrid(gridSpecs);

        int chainCount = GridSpecs!.SerialPorts.Length;
        _sensorData = new byte[chainCount][];
        for (int i = 0; i < chainCount; i++)
        {
            _sensorData[i] = new byte[GridSpecs.SensorMessageLength(i)];
            GridSpecs.SensorHeader(i).CopyTo(_sensorData[i]);
        }
    }

    protected override void MessageReceived(int chainIndex, ReadOnlySpan<byte> message)
    {
        if (GridSpecs == null) { return; }

        const int BYTES_PER_PIXEL = 3;
        int t = 0;
        for (int i = GridSpecs.RgbHeader.Length; i < message.Length - BYTES_PER_PIXEL + 1; i += BYTES_PER_PIXEL)
        {
            var (x, y) = GridSpecs.GridFromChain(chainIndex)[t];
            var r = message[i];
            var g = message[i + 1];
            var b = message[i + 2];
            _updatePixelCallback(x, y, r, g, b);
            ++t;
        }
    }

    public async Task PutSensorData(ISensorView sv)
    {
        if (Assert.ReportNull(sv)) return;
        if (Assert.ReportNull(GridSpecs)) return;

        for (int x = 0; x < GridSpecs.Width; ++x)
        {
            for (int y = 0; y < GridSpecs.Height; ++y)
            {
                var reading = sv.GetTile(x, y);
                var (i, t) = GridSpecs.ChainFromGrid(x, y);
                var offset = GridSpecs.SensorHeader(i).Length;
                _sensorData[i][offset + t] = reading;
            }
        }
        Task[] writes = new Task[_sensorData.Length];
        for (int i = 0; i < _sensorData.Length; ++i)
        {
            writes[i] = WriteChain(i, _sensorData[i]);
        }
        await Task.WhenAll(writes);
    }
}
