using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class InstanceCreatedResponse : IResponse
    {
        public string InstanceId { get; set; } = string.Empty;

        [JsonIgnore]
        public string GetResponseType => ResponseType.InstanceCreated;
    }
}
