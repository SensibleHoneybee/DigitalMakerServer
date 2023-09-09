namespace DigitalMakerApi.Requests
{
    public class GetParticipantsForMeetingRequest
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;
    }
}
