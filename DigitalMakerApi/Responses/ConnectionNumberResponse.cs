using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class ConnectionTestNumberResponse : IResponse
    {
        public string InstanceId = string.Empty;

        public string ConnectionTestNumber = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.ConnectionTestNumber;
    }
}
