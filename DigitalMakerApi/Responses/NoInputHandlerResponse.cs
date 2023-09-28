using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class NoInputHandlerResponse : IResponse
    {
        public string InstanceId { get; set; } = string.Empty;

        public string InputName { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.NoInputHandler;
    }
}
