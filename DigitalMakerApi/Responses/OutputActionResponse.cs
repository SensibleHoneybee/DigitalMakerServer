using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class OutputActionResponse : IResponse
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string OutputName { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.OutputAction;
    }
}
