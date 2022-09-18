using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class InstanceAdminReconnectedResponse : IResponse
    {
        public string InstanceId { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.InstanceAdminReconnected;
    }
}
