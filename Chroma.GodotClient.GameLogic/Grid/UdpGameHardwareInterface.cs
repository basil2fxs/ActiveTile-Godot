using System.Net;
using System.Net.Sockets;
using MysticClue.Chroma.GodotClient.GameLogic;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Implements receiving connections from the hardware as a UDP server.
/// </summary>
public sealed class UdpGameHardwareInterface : IAsyncDisposable, IDisposable, IGameHardwareInterface
{
    private ResolvedGridSpecs _gridSpecs;
    private Dictionary<IPEndPoint, int> _chainFromEndpoint = new();
    private Dictionary<IPEndPoint, bool> _observedEndpoint = new();
    private Task _process;
    private byte[][] _nextFrame;
    private UdpClient _client;
    private bool[] _writeInProgress;
    private CancellationTokenSource _stop;

    public bool AllConnected { get; private set; }

    public IGameHardwareInterface.UpdateSensor? UpdateSensorCallback { get; set; }

    public UdpGameHardwareInterface(ResolvedGridSpecs grid, IPEndPoint localEndpoint)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(localEndpoint);

        _gridSpecs = grid;
        var chainCount = _gridSpecs.OutputPorts.Length;
        _nextFrame = new byte[chainCount][];
        _client = new UdpClient(localEndpoint.Port);
        NetworkInterfaceHelpers.DisableUdpConnectionReset(_client);
        _writeInProgress = new bool[chainCount];
        _stop = new CancellationTokenSource();
        for (int i = 0; i < _nextFrame.Length; ++i)
        {
            _chainFromEndpoint[_gridSpecs.OutputPorts[i]] = i;
            _nextFrame[i] = new byte[_gridSpecs.RgbMessageLength(i)];
            _gridSpecs.RgbHeader.CopyTo(_nextFrame[i]);
        }
        AllConnected = true;
        Console.WriteLine($"Listening on port {localEndpoint.Port}");
        _process = Process();
    }

    private async Task Process()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                var result = await _client.ReceiveAsync(_stop.Token);
                if (_chainFromEndpoint.TryGetValue(result.RemoteEndPoint, out int chainIndex))
                {
                    if (!_observedEndpoint.ContainsKey(result.RemoteEndPoint))
                    {
                        Console.WriteLine($"Heard from endpoint {result.RemoteEndPoint}");
                        _observedEndpoint[result.RemoteEndPoint] = true;
                    }

                    var bytes = result.Buffer;
                    var expectedMessageLength = _gridSpecs.SensorMessageLength(chainIndex);
                    int start = bytes.AsSpan().IndexOf(_gridSpecs.SensorHeader(chainIndex));
                    bool headerNotFound = start < 0;
                    bool messageTooShort = bytes.Length - start < expectedMessageLength;
                    if (headerNotFound || messageTooShort)
                    {
                        continue;
                    }

                    UpdateSensorData(chainIndex, bytes.AsSpan(start, expectedMessageLength));
                }
                else
                {
                    Console.Error.WriteLine($"Received {result.Buffer.Length} bytes from unexpected endpoint {result.RemoteEndPoint}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                if (!_stop.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
        }
    }
    private void UpdateSensorData(int chainIndex, ReadOnlySpan<byte> message)
    {
        if (_gridSpecs == null || UpdateSensorCallback == null) { return; }

        int t = 0;
        for (int i = _gridSpecs.SensorHeader(chainIndex).Length; i < message.Length; ++i)
        {
            var (x, y) = _gridSpecs.GridFromChain(chainIndex)[t];
            UpdateSensorCallback(x, y, message[i]);
            ++t;
        }
    }

    public async Task PutFrame(IFrameView fv)
    {
        if (Assert.ReportNull(fv)) { return; }

        const int BYTES_PER_PIXEL = 3;
        for (int x = 0; x < _gridSpecs.Width; ++x)
        {
            for (int y = 0; y < _gridSpecs.Height; ++y)
            {
                var (r, g, b) = fv.GetPixel(x, y);
                var (i, t) = _gridSpecs.ChainFromGrid(x, y);
                var offset = _gridSpecs.RgbHeader.Length;
                _nextFrame[i][offset + BYTES_PER_PIXEL * t] = r;
                _nextFrame[i][offset + BYTES_PER_PIXEL * t + 1] = g;
                _nextFrame[i][offset + BYTES_PER_PIXEL * t + 2] = b;
            }
        }
        Task[] writes = new Task[_writeInProgress.Length];
        for (int i = 0; i < _writeInProgress.Length; ++i)
        {
            if (!_writeInProgress[i])
            {
                writes[i] = WriteChain(i);
            }
        }
        await Task.WhenAll(writes);
    }

    private async Task WriteChain(int chainIndex)
    {
        _writeInProgress[chainIndex] = true;
        var endpoint = _gridSpecs.OutputPorts[chainIndex];
        try
        {
            await _client.SendAsync(_nextFrame[chainIndex], endpoint, _stop.Token);
        }
        catch (IOException ex) { Console.Error.WriteLine(ex); }
        _writeInProgress[chainIndex] = false;
    }

    public void Dispose()
    {
        ((IDisposable)_stop).Dispose();
        _client.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await _process;
    }
}
