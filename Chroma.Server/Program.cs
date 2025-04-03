using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MysticClue.Chroma.Server.Services.UnityClient;
using MysticClue.Chroma.Server.Settings;
using Serilog.Events;

// Setup Logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

// Setup host
var builder = new HostBuilder()
    .UseConsoleLifetime()
    .UseSerilog();

// Setup app settings configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// Add configuration parsed from app settings
builder.ConfigureAppConfiguration(appConfig =>
{
    appConfig.AddConfiguration(config);
});

// Configure all services for dependency injection
builder.ConfigureServices((context, services) =>
{
    // TODO - Insert all configuration/settings here
    services.Configure<ServerTcpSettings>(context.Configuration.GetRequiredSection(nameof(ServerTcpSettings)));
    
    // TODO - Insert all services and controllers here
    services.AddHostedService<UnityClientService>();
});

// Initialize the host builder
var host = builder.Build();

// Perform all program/app specific initializations here
// i.e., await InitializeLocalCache();

// Run
await host.RunAsync();
Console.WriteLine($"Chroma Service Terminated");



