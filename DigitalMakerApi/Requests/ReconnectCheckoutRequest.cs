namespace DigitalMakerApi.Requests
{
    public class ReconnectCheckoutRequest
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;
    }
}
