namespace DigitalMakerApi.Requests
{
    public class RejoinParticipantWithPasswordRequest
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;

        public string ParticipantId { get; set; } = string.Empty;

        public string ParticipantPassword { get; set; } = string.Empty;

        public string NewLoginCipher { get; set; } = string.Empty;
    }
}
