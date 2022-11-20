using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class ConnectedMeetingAdminResponse : IResponse
    {
        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.ConnectedMeetingAdmin;
    }
}
