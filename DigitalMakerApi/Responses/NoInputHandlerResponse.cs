using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class NoInputHandlerResponse : IResponse
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string InputName { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.NoInputHandler;
    }
}
