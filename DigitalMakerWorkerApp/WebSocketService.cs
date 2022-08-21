using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DigitalMakerWorkerApp
{
    public class WebSocketService
    {
        private readonly ILogger<WebSocketService> _logger;
        private readonly ClientWebSocket _wsClient = new ClientWebSocket();

        public WebSocketService(ILogger<WebSocketService> logger)
        {
            this._logger = logger;
        }

        public async Task OpenConnectionAsync(CancellationToken token)
        {
            var secretsData = GetDataFromSecretsFile();

            if (secretsData == null)
            {
                return;
            }

            //Set keep alive interval
            this._wsClient.Options.KeepAliveInterval = TimeSpan.Zero;

            await this._wsClient.ConnectAsync(new Uri(secretsData.Url), token).ConfigureAwait(false);
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

        private Secrets? GetDataFromSecretsFile()
        {
            var homePath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                            ? Environment.GetEnvironmentVariable("HOME")
                            : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            if (homePath == null)
            {
                return null;
            }

            var secretsFileLocation = Path.Combine(homePath, @"source\repos\Secrets.json");
            var secrets = File.ReadAllText(secretsFileLocation);

            JsonDocument message = JsonDocument.Parse(secrets);

            JsonElement digitalMakerUrlProperty;
            if (!message.RootElement.TryGetProperty("DigitalMakerUrl", out digitalMakerUrlProperty))
            {
                _logger.LogWarning("Failed to find DigitalMakerUrl element in Secrets JSON document");
                return null;
            }
            var digitalMakerUrl = digitalMakerUrlProperty.GetString();
            if (string.IsNullOrWhiteSpace(digitalMakerUrl))
            {
                _logger.LogWarning("DigitalMakerUrl element in Secrets JSON document is blank");
                return null;
            }

            JsonElement authenticationTokenProperty;
            if (!message.RootElement.TryGetProperty("AuthenticationToken", out authenticationTokenProperty))
            {
                _logger.LogWarning("Failed to find AuthenticationToken element in Secrets JSON document");
                return null;
            }
            var authenticationToken = authenticationTokenProperty.GetString();
            if (string.IsNullOrWhiteSpace(authenticationToken))
            {
                _logger.LogWarning("AuthenticationToken element in Secrets JSON document is blank");
                return null;
            }

            return new Secrets(digitalMakerUrl, authenticationToken);
        }

        private record Secrets(string Url, string AuthenticationToken);
    }
}