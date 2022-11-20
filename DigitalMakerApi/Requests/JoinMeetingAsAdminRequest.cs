namespace DigitalMakerApi.Requests
{
    public class JoinMeetingAsAdminRequest
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingAdminPassword { get; set; } = string.Empty;
    }
}
