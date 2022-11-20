using Amazon.DynamoDBv2.DataModel;

namespace DigitalMakerServer
{
    public class MeetingAdminStorage
    {
        public MeetingAdminStorage()
        {
            this.Id = string.Empty;
            this.MeetingAdminPasswordHash = string.Empty;
        }

        public string Id { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        public string MeetingAdminPasswordHash { get; set; }

        public string MeetingAdminConnectionId { get; set; } = string.Empty;

        [DynamoDBVersion]
        public long? Version { get; set; }
    }
}
