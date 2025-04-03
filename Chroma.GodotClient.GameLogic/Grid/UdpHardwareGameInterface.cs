using System.Net;
using System.Net.Sockets;
using MysticClue.Chroma.GodotClient.GameLogic;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Implements connection to the game as a UDP client.
/// </summary>
public sealed class UdpHardwareGameInterface : IAsyncDisposable, IDisposable, IHardwareGameInterface
{
    private ResolvedGridSpecs? _gridSpecs;

    IPEndPoint _gameSideEndpoint;
    private Dictionary<IPEndPoint, CancellationTokenSource> _cancel = new();
    private Dictionary<IPEndPoint, int> _chainFromEndpoint = new();
    private Task[] _processes;
    private byte[][] _sensorData;
    private UdpClient[] _clients;
    private bool[] _writeInProgress;

    public delegate void UpdatePixel(int x, int y, byte r, byte g, byte b);
    private UpdatePixel _updatePixelCallback;

    public UdpHardwareGameInterface(IPEndPoint gameSideEndpoint, UpdatePixel updatePixelCallback)
    {
        _gameSideEndpoint = gameSideEndpoint;
        _processes = [];
        _sensorData = [];
        _clients = [];
        _writeInProgress = [];
        _updatePixelCallback = updatePixelCallback;
    }

    public void UpdateGrid(ResolvedGridSpecs gridSpecs)
    {
        _gridSpecs = gridSpecs;
        int chainCount = _gridSpecs.OutputPorts.Length;
        _chainFromEndpoint.Clear();
        _processes = new Task[chainCount];
        _sensorData = new byte[chainCount][];
        _clients = new UdpClient[chainCount];
        _writeInProgress = new bool[chainCount];
        for (int i = 0; i < chainCount; ++i)
        {
            var endpoint = _gridSpecs.OutputPorts[i];
            _chainFromEndpoint[endpoint] = i;
            if (!_cancel.ContainsKey(endpoint))
            {
                _cancel[endpoint] = new CancellationTokenSource();
                // Start it here so we propagate any exceptions immediately.
                _processes[i] = Process(i, endpoint);
            }
            _sensorData[i] = new byte[_gridSpecs.SensorMessageLength(i)];
            _gridSpecs.SensorHeader(i).CopyTo(_sensorData[i]);
        }
        foreach (var hostport in _cancel.Keys)
        {
            if (!_chainFromEndpoint.ContainsKey(hostport))
            {
                _cancel[hostport].Cancel();
                _cancel.Remove(hostport);
            }
        }
    }

    private async Task Process(int chainIndex, IPEndPoint endpoint)
    {
        try
        {
            if (_gridSpecs == null) { return; }

            int expectedMessageLength = _gridSpecs.RgbMessageLength(chainIndex);

            using UdpClient client = new(endpoint.Port);
            NetworkInterfaceHelpers.DisableUdpConnectionReset(client);
            _clients[chainIndex] = client;
            Console.WriteLine($"Listening on port {endpoint.Port}");
            while (!_cancel[endpoint].IsCancellationRequested)
            {
                var result = await client.ReceiveAsync(_cancel[endpoint].Token);

                // Ignore this datagram if it doesn't contain a complete message.
                byte[] bytes = result.Buffer;
                int start = bytes.AsSpan().IndexOf(_gridSpecs.RgbHeader);
                if (start < 0 || bytes.Length - start < expectedMessageLength)
                {
                    continue;
                }

                UpdateGridColors(chainIndex, bytes.AsSpan(start, expectedMessageLength));
            }
        }
        // Since Process() is not awaited until DisposeAsync(), exceptions above are not
        // made visible anywhere, so catch and print all exceptions.
        catch (Exception e) { Console.WriteLine(e.ToString()); }
    }
    private void UpdateGridColors(int chainIndex, ReadOnlySpan<byte> message)
    {
        if (_gridSpecs == null) { return; }

        const int BYTES_PER_PIXEL = 3;
        int t = 0;
        for (int i = _gridSpecs.RgbHeader.Length; i < message.Length - BYTES_PER_PIXEL + 1; i += BYTES_PER_PIXEL)
        {
            var (x, y) = _gridSpecs.GridFromChain(chainIndex)[t];
            var r = message[i];
            var g = message[i + 1];
            var b = message[i + 2];
            _updatePixelCallback(x, y, r, g, b);
            ++t;
        }
    }

    public async Task PutSensorData(ISensorView sv)
    {
        if (_gridSpecs == null) { return; }

        for (int x = 0; x < _gridSpecs.Width; ++x)
        {
            for (int y = 0; y < _gridSpecs.Height; ++y)
            {
                var reading = sv.GetTile(x, y);
                var (i, t) = _gridSpecs.ChainFromGrid(x, y);
                var offset = _gridSpecs.SensorHeader(i).Length;
                _sensorData[i][offset + t] = reading;
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
        if (_gridSpecs == null) { return; }

        _writeInProgress[chainIndex] = true;
        var cancel = _cancel[_gridSpecs.OutputPorts[chainIndex]];
        try
        {
            await _clients[chainIndex].SendAsync(_sensorData[chainIndex], _gameSideEndpoint, cancel.Token);
        }
        catch (IOException ex) { Console.Error.WriteLine(ex); }
        _writeInProgress[chainIndex] = false;
    }

    public void Dispose()
    {
        foreach (IDisposable cancel in _cancel.Values)
        {
            cancel.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.WhenAll(_processes);
    }
}
