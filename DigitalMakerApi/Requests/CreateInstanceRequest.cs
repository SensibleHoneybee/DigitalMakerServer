namespace DigitalMakerApi.Requests
{
    public class CreateInstanceRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public string InstanceName { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;

        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;
    }
}
