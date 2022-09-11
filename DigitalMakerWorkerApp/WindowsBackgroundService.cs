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
        try
        {
            _logger.LogInformation("WindowsBackgroundService: starting web socket client");

            ////this._webSocketService = this._webSocketService.Create(url);

            await this._webSocketService.OpenConnectionAsync(stoppingToken);

            this._webSocketService.HandleMessagesAsync(stoppingToken).ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            var createInstanceRequest = new CreateInstanceRequest
            {
                InstanceId = Guid.NewGuid().ToString(),
                InstanceName = "Test Instance 1"
            };
            var requestWrapper = new RequestWrapper { RequestType = RequestType.CreateInstance, Content = JsonConvert.SerializeObject(createInstanceRequest) };
            var webSocketRequest = new { message = "sendmessage", data = JsonConvert.SerializeObject(requestWrapper) };
            await this._webSocketService.SendAsync(JsonConvert.SerializeObject(webSocketRequest), stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}