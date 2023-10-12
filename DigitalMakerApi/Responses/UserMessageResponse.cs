using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class UserMessageResponse : IResponse
    {
        public string InstanceId = string.Empty;

        public string Message = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.UserMessage;
    }
}
