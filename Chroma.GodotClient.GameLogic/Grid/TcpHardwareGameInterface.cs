using System.Net;
using System.Net.Sockets;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Implements receiving connections from the game as a TCP server.
///
/// TODO: Implement sending back presses.
/// </summary>
public sealed class TcpHardwareGameInterface : IHardwareGameInterface
{
    private ResolvedGridSpecs? _gridSpecs;

    private Dictionary<int, CancellationTokenSource> _servers = new();
    private Dictionary<int, int> _chainFromServer = new();
    private Dictionary<int, bool> _portsConnected = new();
    private Task[] _processes;

    public delegate void Callback(int x, int y, byte r, byte g, byte b);
    private Callback _updatePixelCallback;

    public TcpHardwareGameInterface(Callback updatePixelCallback)
    {
        _processes = [];
        _updatePixelCallback = updatePixelCallback;
    }

    public void UpdateGrid(ResolvedGridSpecs gridSpecs)
    {
        _gridSpecs = gridSpecs;
        _chainFromServer = new();
        _processes = new Task[_gridSpecs.OutputPorts.Length];
        for (int i = 0; i < _gridSpecs.OutputPorts.Length; ++i)
        {
            var port = _gridSpecs.OutputPorts[i].Port;
            _chainFromServer[port] = i;
            if (!_servers.ContainsKey(port))
            {
                var s = new TcpListener(IPAddress.Any, port);
                var c = new CancellationTokenSource();
                _servers[port] = c;
                _portsConnected[port] = false;
                // Start it here so we propagate any exceptions immediately.
                s.Start();
                _processes[i] = ProcessTcp(i, port, s, c.Token);
            }
        }
        foreach (var port in _servers.Keys)
        {
            if (!_chainFromServer.ContainsKey(port))
            {
                _servers[port].Cancel();
                _servers.Remove(port);
                _portsConnected.Remove(port);
            }
        }
    }

    private async Task ProcessTcp(int chainIndex, int port, TcpListener server, CancellationToken cancel)
    {
        try
        {
            if (_gridSpecs == null) { return; }

            StreamMessageReader reader = new(
                _gridSpecs.RgbHeader.ToArray(),
                _gridSpecs.RgbMessageLength(chainIndex),
                message => UpdateGridColors(chainIndex, message));

            while (!cancel.IsCancellationRequested)
            {
                _portsConnected[port] = false;
                Console.WriteLine($"Ports connected: {string.Join(' ', _portsConnected)}");
                using TcpClient client = await server.AcceptTcpClientAsync(cancel);
                _portsConnected[port] = true;
                Console.WriteLine($"Ports connected: {string.Join(' ', _portsConnected)}");

                NetworkStream stream = client.GetStream();
                // TODO: also write back sensor data, probably need 2 async tasks.
                await reader.ReadStream(stream, cancel);
            }
        }
        catch (IOException e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            server.Stop();
            server.Dispose();
        }
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
        /// TODO: Implement sending back presses.
        await Task.Delay(1);
    }

    public void Dispose()
    {
        foreach (IDisposable cancel in _servers.Values) cancel.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.WhenAll(_processes);
    }
}
