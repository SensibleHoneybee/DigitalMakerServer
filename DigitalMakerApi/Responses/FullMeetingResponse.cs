using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class FullMeetingResponse : IResponse
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingName { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.FullMeeting;
    }
}
