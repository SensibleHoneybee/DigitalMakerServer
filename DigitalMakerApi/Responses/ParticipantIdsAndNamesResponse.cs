using DigitalMakerApi.Models;
using System.Text.Json.Serialization;

namespace DigitalMakerApi.Responses
{
    public class ParticipantIdsAndNamesResponse : IResponse
    {
        public List<ParticipantIdAndName> ParticipantIdsAndNames { get; set; } = new List<ParticipantIdAndName>();

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.MeetingWithParticipant;
    }
}
