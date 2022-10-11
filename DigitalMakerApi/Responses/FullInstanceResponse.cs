using DigitalMakerApi.Models;
using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class FullInstanceResponse : IResponse
    {
        public Instance Instance { get; set; } = null!;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.FullInstance;
    }
}
