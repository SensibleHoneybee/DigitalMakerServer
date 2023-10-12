namespace DigitalMakerApi.Requests
{
    public class ConnectInputOutputDeviceRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public List<string> OutputReceiverNames { get; set; } = new List<string>();
    }
}
