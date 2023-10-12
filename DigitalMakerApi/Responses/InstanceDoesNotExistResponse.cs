using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class InstanceDoesNotExistResponse : IResponse
    {
        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.InstanceDoesNotExist;
    }
}
