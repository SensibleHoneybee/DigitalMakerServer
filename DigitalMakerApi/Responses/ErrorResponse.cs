using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class ErrorResponse : IResponse
    {
        public string Message { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.Error;
    }
}
