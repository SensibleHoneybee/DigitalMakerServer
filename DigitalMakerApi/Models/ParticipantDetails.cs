namespace DigitalMakerApi.Models
{
    public class ParticipantDetails
    {
        public string ParticipantId { get; set; } = string.Empty;

        public string ParticipantNames { get; set; } = string.Empty;

        public string ParticipantPasswordHash { get; set; } = string.Empty;

        public string LoginCipher { get; set; } = string.Empty;

        public string DefaultInstanceId { get; set; } = string.Empty;
    }
}
