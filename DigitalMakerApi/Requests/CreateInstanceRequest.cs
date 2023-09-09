namespace DigitalMakerApi.Requests
{
    public class GetOrCreateInstanceRequest
    {
        public string MeetingId { get; set; } = string.Empty;

        public string ParticipantId { get; set; } = string.Empty;

        public string LoginCipher { get; set; } = string.Empty;
    }
}
