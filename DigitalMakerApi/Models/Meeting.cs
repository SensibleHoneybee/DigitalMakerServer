namespace DigitalMakerApi.Models
{
    public class Meeting
    {
        public string MeetingName { get; set; } = string.Empty;

        public string MeetingPasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public List<ParticipantDetails> Participants { get; set; } = new List<ParticipantDetails>();
    }
}
