using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class InstanceCreatedResponse : IResponse
    {
        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.InstanceCreated;
    }
}
