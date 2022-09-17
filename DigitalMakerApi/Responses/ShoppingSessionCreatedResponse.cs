using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class ShoppingSessionCreatedResponse : IResponse
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.ShoppingSessionCreated;
    }
}
