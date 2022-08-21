using DigitalMakerWorkerApp;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var argCounter = 0;
string? url = null;
while (argCounter < args.Length)
{
    var nextArg = args[argCounter++];
    if (nextArg != null && nextArg == "-url")
    {
        if (argCounter < args.Length)
        {
            url = args[argCounter++];
            continue;
        }
    }

    // If we get here, an unknown parameter or missing URL etc. was detected
    Console.WriteLine("Invalid arguments.\r\nUsage: DigitalMakerWorkerApp.exe -url <URL>");
    return;
}

if (url == null)
{
    Console.WriteLine("You must specify a URL.\r\nUsage: DigitalMakerWorkerApp.exe -url <URL>");
    return;
}

using IHost host = Host.CreateDefaultBuilder()
    .UseWindowsService(options =>
    {
        options.ServiceName = "DigitalMaker Worker App Service";
    })
    .ConfigureServices(services =>
    {
        LoggerProviderOptions.RegisterProviderOptions<
            EventLogSettings, EventLogLoggerProvider>(services);

        services.AddSingleton<WebSocketServiceFactory>();
        services.AddHostedService<WindowsBackgroundService>();
    })
    .ConfigureLogging((context, logging) =>
    {
        // See: https://github.com/dotnet/runtime/issues/47303
        logging.AddConfiguration(
            context.Configuration.GetSection("Logging"));
    })
    .Build();

await host.RunAsync();