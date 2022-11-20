namespace DigitalMakerApi.Requests
{
    public class InputReceivedRequest
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string InputName { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;
    }
}
