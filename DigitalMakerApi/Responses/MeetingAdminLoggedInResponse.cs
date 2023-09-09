using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class MeetingAdminLoggedInResponse : IResponse
    {
        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.MeetingAdminLoggedIn;
    }
}
