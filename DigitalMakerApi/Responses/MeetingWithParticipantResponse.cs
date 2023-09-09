using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class MeetingWithParticipantResponse : IResponse
    {
        public string MeetingId { get; set; } = string.Empty;

        public string MeetingName { get; set; } = string.Empty;

        public string ParticipantId { get; set; } = string.Empty;

        public string ParticipantNames { get; set; } = string.Empty;

        public string LoginCipher { get; set; } = string.Empty;

        public string DefaultInstanceId { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.MeetingWithParticipant;
    }
}
