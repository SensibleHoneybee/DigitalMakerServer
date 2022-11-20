namespace DigitalMakerApi.Requests
{
    public class CreateMeetingRequest
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingName { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;

        public string MeetingAdminPassword { get; set; } = string.Empty;
    }
}
