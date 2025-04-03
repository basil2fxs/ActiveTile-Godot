using MysticClue.Chroma.GodotClient.GameLogic;
using System.Net;
using System.Net.Sockets;

namespace MysticClue.Chroma.PortForwarder;

public sealed class UdpPortForwarder : IAsyncDisposable, IDisposable
{
    IPEndPoint _endpoint;
    IPEndPoint _target;
    UdpClient _client;
    Dictionary<IPEndPoint, UdpClient> _inboundRemoteMirrors;

    List<Task> _processes;
    CancellationTokenSource _stop;

    public UdpPortForwarder(IPAddress local, int port, IPAddress target)
    {
        _endpoint = new IPEndPoint(local, port);
        _target = new IPEndPoint(target, port);
        _client = new UdpClient(_endpoint);
        NetworkInterfaceHelpers.DisableUdpConnectionReset(_client);
        _inboundRemoteMirrors = new();
        _processes = new();
        _stop = new();

        _processes.Add(ProcessInbound());
    }

    public async Task ProcessInbound()
    {
        Console.WriteLine($"Listening for hardware on {_endpoint}");
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                var result = await _client.ReceiveAsync(_stop.Token);

                if (!_inboundRemoteMirrors.TryGetValue(result.RemoteEndPoint, out var mirror))
                {
                    var client = new UdpClient(new IPEndPoint(IPAddress.Any, result.RemoteEndPoint.Port));
                    NetworkInterfaceHelpers.DisableUdpConnectionReset(client);
                    mirror = client;
                    _processes.Add(ProcessOutbound(result.RemoteEndPoint));
                }

                await _inboundRemoteMirrors[result.RemoteEndPoint].SendAsync(result.Buffer, _target, _stop.Token);
            }
            // Ignore exceptions due to shutdown.
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (SocketException ex) { Console.WriteLine(ex); }
        }
    }

    public async Task ProcessOutbound(IPEndPoint returnEndpoint)
    {
        Console.WriteLine($"Got response from hardware and listening for game on {returnEndpoint}");
        bool heardFromGame = false;
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                var result = await _inboundRemoteMirrors[returnEndpoint].ReceiveAsync(_stop.Token);
                if (!result.RemoteEndPoint.Equals(_target))
                {
                    Console.WriteLine($"Received from unexpected endpoint {result.RemoteEndPoint} {_target}");
                }

                if (!heardFromGame)
                {
                    Console.WriteLine($"Got response from game on {returnEndpoint}");
                    heardFromGame = true;
                }
                await _client.SendAsync(result.Buffer, returnEndpoint, _stop.Token);
            }
            // Ignore exceptions due to shutdown.
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (SocketException ex) { Console.WriteLine(ex); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("Stopping.");
        _stop.Cancel();
        await Task.WhenAll(_processes);
        Dispose();
    }

    public void Dispose()
    {
        ((IDisposable)_stop).Dispose();
        _client.Dispose();
        foreach (var client in _inboundRemoteMirrors.Values)
        {
            client.Dispose();
        }
    }
}
