namespace DigitalMakerApi.Requests
{
    public class ReconnectInstanceAdminRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;
    }
}
