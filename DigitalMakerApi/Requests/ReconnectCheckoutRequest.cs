namespace DigitalMakerApi.Requests
{
    public class ReconnectCheckoutRequest
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string MeetingId { get; set; } = string.Empty;

        public string ParticipantId { get; set; } = string.Empty;

        public string LoginCipher { get; set; } = string.Empty;
    }
}
