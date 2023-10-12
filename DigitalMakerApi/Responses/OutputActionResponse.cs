using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class OutputActionResponse : IResponse
    {
        public string InstanceId { get; set; } = string.Empty;

        public string OutputName { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.OutputAction;
    }
}
