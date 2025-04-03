using System.Diagnostics;
using System.Net.Sockets;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Implements connecting to the hardware as a TCP client.
///
/// TODO: Implement receiving presses.
/// </summary>
public sealed class TcpGameHardwareInterface : IDisposable, IGameHardwareInterface
{
    private ResolvedGridSpecs _gridSpecs;
    private byte[][] _nextFrame;
    private TcpClient[] _clients;
    private bool[] _writeInProgress;
    private CancellationTokenSource _stopClients;
    private Task[] _processes;

    public bool AllConnected => _clients.All(c => c.Connected);

    public IGameHardwareInterface.UpdateSensor? UpdateSensorCallback { get; set; }

    public TcpGameHardwareInterface(ResolvedGridSpecs grid)
    {
        _gridSpecs = grid;
        var chainCount = _gridSpecs.OutputPorts.Length;
        _nextFrame = new byte[chainCount][];
        _clients = new TcpClient[chainCount];
        _writeInProgress = new bool[chainCount];
        _stopClients = new CancellationTokenSource();
        _processes = new Task[chainCount];
        for (int i = 0; i < _nextFrame.Length; ++i)
        {
            _nextFrame[i] = new byte[_gridSpecs.RgbMessageLength(i)];
            _gridSpecs.RgbHeader.CopyTo(_nextFrame[i]);
            _clients[i] = new TcpClient();
            _processes[i] = ProcessTcp(i);
        }
    }

    private async Task ProcessTcp(int chainIndex)
    {
        var client = _clients[chainIndex];
        while (!_stopClients.IsCancellationRequested && !client.Connected)
        {
            var endpoint = _gridSpecs.OutputPorts[chainIndex];

            try
            {
                await client.ConnectAsync(endpoint, _stopClients.Token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                await Task.Delay(1000);
            }
        }
    }

    public async Task PutFrame(IFrameView fv)
    {
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
        Task[] writes = new Task[_clients.Length];
        for (int i = 0; i < _clients.Length; ++i)
        {
            if (_clients[i].Connected && !_writeInProgress[i])
            {
                writes[i] = WriteChain(i);
            }
        }
        await Task.WhenAll(writes);
    }

    private async Task WriteChain(int chainIndex)
    {
        _writeInProgress[chainIndex] = true;
        var stream = _clients[chainIndex].GetStream();
        try { await stream.WriteAsync(_nextFrame[chainIndex], _stopClients.Token); }
        catch (IOException ex) { Console.Error.WriteLine(ex); }
        _writeInProgress[chainIndex] = false;
    }

    public void Dispose()
    {
        ((IDisposable)_stopClients).Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.WhenAll(_processes);
    }
}
