using MysticClue.Chroma.GodotClient.GameLogic.Debugging;
using MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Implements connecting to the hardware on a serial port.
/// </summary>
public sealed class SerialGameHardwareInterface : SerialMessageInterface, IGameHardwareInterface
{
    private byte[][] _nextFrame;

    protected override ReadOnlySpan<byte> InboundMessageHeader(int chainIndex) => GridSpecs!.SensorHeader(chainIndex);
    protected override int InboundMessageLength(int chainIndex) => GridSpecs!.SensorMessageLength(chainIndex);

    public bool AllConnected { get; private set; }

    public IGameHardwareInterface.UpdateSensor? UpdateSensorCallback { get; set; }

    public SerialGameHardwareInterface(ResolvedGridSpecs grid, SerialPortFactory portFactory) : base(portFactory)
    {
        ArgumentNullException.ThrowIfNull(grid);

        UpdateGrid(grid);
        int chainCount = GridSpecs!.SerialPorts.Length;
        _nextFrame = new byte[chainCount][];
        for (int i = 0; i < chainCount; i++)
        {
            _nextFrame[i] = new byte[GridSpecs.RgbMessageLength(i)];
            GridSpecs.RgbHeader.CopyTo(_nextFrame[i]);
        }
        AllConnected = true;
    }

    protected override void MessageReceived(int chainIndex, ReadOnlySpan<byte> message)
    {
        if (GridSpecs == null || UpdateSensorCallback == null) { return; }

        int t = 0;
        for (int i = GridSpecs.SensorHeader(chainIndex).Length; i < message.Length; ++i)
        {
            var (x, y) = GridSpecs.GridFromChain(chainIndex)[t];
            UpdateSensorCallback(x, y, message[i]);
            ++t;
        }
    }

    public async Task PutFrame(IFrameView fv)
    {
        if (Assert.ReportNull(fv)) return;
        if (Assert.ReportNull(GridSpecs)) return;

        const int BYTES_PER_PIXEL = 3;
        for (int x = 0; x < GridSpecs.Width; ++x)
        {
            for (int y = 0; y < GridSpecs.Height; ++y)
            {
                var (r, g, b) = fv.GetPixel(x, y);
                var (i, t) = GridSpecs.ChainFromGrid(x, y);
                var offset = GridSpecs.RgbHeader.Length;
                _nextFrame[i][offset + BYTES_PER_PIXEL * t] = r;
                _nextFrame[i][offset + BYTES_PER_PIXEL * t + 1] = g;
                _nextFrame[i][offset + BYTES_PER_PIXEL * t + 2] = b;
            }
        }
        Task[] writes = new Task[_nextFrame.Length];
        for (int i = 0; i < _nextFrame.Length; ++i)
        {
            writes[i] = WriteChain(i, _nextFrame[i]);
        }
        await Task.WhenAll(writes);
    }
}
