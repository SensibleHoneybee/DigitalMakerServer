using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class SuccessResponse : IResponse
    {
        public string Message = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.Success;
    }
}
