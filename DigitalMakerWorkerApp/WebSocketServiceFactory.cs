namespace DigitalMakerWorkerApp
{
    public class WebSocketServiceFactory
    {
        private readonly ILogger<WebSocketService> _logger;

        public WebSocketServiceFactory(ILogger<WebSocketService> logger)
        {
            this._logger = logger;
        }

        public WebSocketService Create(string url)
        {
            return new WebSocketService(url, this._logger);
        }
    }
}