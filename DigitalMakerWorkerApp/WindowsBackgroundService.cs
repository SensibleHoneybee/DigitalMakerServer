using DigitalMakerApi;
using DigitalMakerApi.Requests;
using Newtonsoft.Json;

namespace DigitalMakerWorkerApp;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly WebSocketService _webSocketService;
    private readonly ILogger<WindowsBackgroundService> _logger;

    public WindowsBackgroundService(
        WebSocketService webSocketService,
        ILogger<WindowsBackgroundService> logger) =>
        (_webSocketService, _logger) = (webSocketService, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}