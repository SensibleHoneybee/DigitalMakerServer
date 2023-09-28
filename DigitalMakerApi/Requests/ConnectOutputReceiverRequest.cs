namespace DigitalMakerApi.Requests
{
    public class ConnectOutputReceiverRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public string OutputReceiverName { get; set; } = string.Empty;

        public string ConnectionId { get; set; } = string.Empty;
    }
}
