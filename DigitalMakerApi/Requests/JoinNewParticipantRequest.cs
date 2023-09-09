namespace DigitalMakerApi.Requests
{
    public class JoinNewParticipantRequest
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;

        public string ParticipantId { get; set; } = string.Empty;

        public string ParticipantNames { get; set; } = string.Empty;

        public string ParticipantPassword { get; set; } = string.Empty;

        public string LoginCipher { get; set; } = string.Empty;
    }
}
