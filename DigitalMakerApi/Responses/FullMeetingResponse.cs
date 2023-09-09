using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class MeetingOnlyResponse : IResponse
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingName { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.MeetingOnly;
    }
}
