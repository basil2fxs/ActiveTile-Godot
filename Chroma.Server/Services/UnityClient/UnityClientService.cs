using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MysticClue.Chroma.Server.Settings;

namespace MysticClue.Chroma.Server.Services.UnityClient;

public class UnityClientService : BackgroundService 
{
    private readonly ILogger<UnityClientService> _logger;
    private readonly ServerTcpSettings _serverTcpSettings;

    public UnityClientService(ILogger<UnityClientService> logger, IOptions<ServerTcpSettings> settings)
    {
        _logger = logger;
        _serverTcpSettings = settings.Value;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Code reference: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=net-8.0
        var server = new TcpListener(IPAddress.Parse(_serverTcpSettings.Host), _serverTcpSettings.Port);
        server.Start();
        server.Start(10);
        
        // TODO - Currently using sockets. See how we can use GRPC for standardized communication objects
        while (true)
        {
            try
            {
                using var client = await server.AcceptTcpClientAsync(stoppingToken);
                await using var stream = client.GetStream();

                int i;
                var bytes = new Byte[256];
                while((i = await stream.ReadAsync(bytes, stoppingToken)) !=0)
                {
                    var data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    _logger.LogDebug("Received: {0}", data);  
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception caught while listening to client requests:\n" + 
                                 $"{ex}");
            }
            finally
            {
                server.Stop();
            }
        }
    }
}