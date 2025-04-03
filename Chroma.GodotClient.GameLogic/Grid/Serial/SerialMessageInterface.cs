using MysticClue.Chroma.GodotClient.GameLogic.Grid.Serial;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// Implements sending and receiving messages on a serial port.
/// </summary>
public class SerialMessageInterface : IAsyncDisposable, IDisposable
{
    private SerialPortFactory _portFactory;

    protected ResolvedGridSpecs? GridSpecs { get; private set; }

    private Dictionary<string, CancellationTokenSource> _cancel = [];
    private Dictionary<string, int> _chainFromPort = [];
    private Task[] _processes;
    private ISerialPort[] _ports;
    private bool[] _writeInProgress;
    private bool disposedValue;

    protected virtual ReadOnlySpan<byte> InboundMessageHeader(int chainIndex) => throw new NotImplementedException();
    protected virtual int InboundMessageLength(int chainIndex) => throw new NotImplementedException();
    protected virtual void MessageReceived(int chainIndex, ReadOnlySpan<byte> message) => throw new NotImplementedException();

    public SerialMessageInterface(SerialPortFactory portFactory)
    {
        _portFactory = portFactory;
        _processes = [];
        _ports = [];
        _writeInProgress = [];
    }

    public virtual void UpdateGrid(ResolvedGridSpecs gridSpecs)
    {
        GridSpecs = gridSpecs;
        int chainCount = GridSpecs.SerialPorts.Length;
        _chainFromPort.Clear();
        _processes = new Task[chainCount];
        _ports = new ISerialPort[chainCount];
        _writeInProgress = new bool[chainCount];
        for (int i = 0; i < chainCount; ++i)
        {
            var port = GridSpecs.SerialPorts[i];
            _chainFromPort[port] = i;
            if (!_cancel.ContainsKey(port))
            {
                _cancel[port] = new CancellationTokenSource();
                _processes[i] = Process(i, port);
            }
        }
        foreach (var port in _cancel.Keys)
        {
            if (!_chainFromPort.ContainsKey(port))
            {
                _cancel[port].Cancel();
                _cancel.Remove(port);
            }
        }
    }

    private async Task Process(int chainIndex, string port)
    {
        try
        {
            if (GridSpecs == null) { return; }

            StreamMessageReader reader = new(
                InboundMessageHeader(chainIndex).ToArray(),
                InboundMessageLength(chainIndex),
                message => MessageReceived(chainIndex, message));

            using ISerialPort serialPort = _portFactory.MakeSerialPort(port);
            Console.WriteLine($"Opened port {port}");
            _ports[chainIndex] = serialPort;
            while (!_cancel[port].IsCancellationRequested)
            {
                await reader.ReadStream(serialPort.Stream, _cancel[port].Token);
            }
        }
        // Catch all exceptions thrown by System.IO.Ports.SerialPort.Open().
        catch (UnauthorizedAccessException ex) { Console.WriteLine(ex); }
        catch (ArgumentOutOfRangeException ex) { Console.WriteLine(ex); }
        catch (ArgumentException ex) { Console.WriteLine(ex); }
        catch (IOException ex) { Console.WriteLine(ex); }
        catch (InvalidOperationException ex) { Console.WriteLine(ex); }
    }

    protected async Task WriteChain(int chainIndex, ReadOnlyMemory<byte> message)
    {
        if (GridSpecs == null) return;

        if (_writeInProgress[chainIndex]) return;

        _writeInProgress[chainIndex] = true;
        var cancel = _cancel[GridSpecs.SerialPorts[chainIndex]];
        try
        {
            if (_ports[chainIndex] != null)
                await _ports[chainIndex].Stream.WriteAsync(message, cancel.Token);
        }
        catch (IOException ex) { Console.Error.WriteLine(ex); }
        _writeInProgress[chainIndex] = false;
    }

    public async ValueTask DisposeAsync()
    {
        Dispose(disposing: true);
        await Task.WhenAll(_processes);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (IDisposable cancel in _cancel.Values) cancel.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
