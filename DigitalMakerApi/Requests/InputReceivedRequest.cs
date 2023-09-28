namespace DigitalMakerApi.Requests
{
    public class InputReceivedRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public string InputName { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;
    }
}
