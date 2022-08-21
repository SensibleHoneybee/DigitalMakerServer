using System.Net.WebSockets;
using System.Text;

namespace DigitalMakerWorkerApp
{
    public class WebSocketService
    {
        private readonly string _url;
        private readonly ILogger<WebSocketService> _logger;
        private readonly ClientWebSocket _wsClient = new ClientWebSocket();

        public WebSocketService(string url, ILogger<WebSocketService> logger)
        {
            this._url = url;
            this._logger = logger;
        }

        public async Task OpenConnectionAsync(CancellationToken token)
        {
            //Set keep alive interval
            this._wsClient.Options.KeepAliveInterval = TimeSpan.Zero;

            await this._wsClient.ConnectAsync(new Uri(_url), token).ConfigureAwait(false);
        }

        //Send message
        public async Task SendAsync(string message, CancellationToken token)
        {
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            await this._wsClient.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
        }

        //Receiving messages
        private async Task ReceiveMessageAsync(byte[] buffer)
        {
            while (true)
            {
                try
                {
                    var result = await this._wsClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);

                    //Here is the received message as string
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (result.EndOfMessage) break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in receiving messages: {err}", ex.Message);
                    break;
                }
            }
        }

        public async Task HandleMessagesAsync(CancellationToken token)
        {
            var buffer = new byte[1024 * 4];
            while (this._wsClient.State == WebSocketState.Open)
            {
                await ReceiveMessageAsync(buffer);
            }
            if (this._wsClient.State != WebSocketState.Open)
            {
                _logger.LogInformation("Connection closed. Status: {s}", this._wsClient.State.ToString());
                // Your logic if state is different than `WebSocketState.Open`
            }
        }
    }
}