namespace DigitalMakerApi.Requests
{
    public class StartOrStopRunningRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public bool Run { get; set; } = false;
    }
}
