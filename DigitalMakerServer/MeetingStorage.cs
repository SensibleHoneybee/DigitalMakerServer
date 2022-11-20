using Amazon.DynamoDBv2.DataModel;

namespace DigitalMakerServer
{
    public class MeetingStorage
    {
        public MeetingStorage()
        {
            this.Id = string.Empty;
            this.MeetingName = string.Empty;
            this.MeetingPasswordHash = string.Empty;
        }

        public string Id { get; set; }

        ////[DynamoDBGlobalSecondaryIndexHashKey("InstanceIndex")]
        ////public string InstanceCode { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        public string MeetingName { get; set; }

        public string MeetingPasswordHash { get; set; }

        public bool IsActive { get; set; }

        public string MeetingAdminConnectionId { get; set; } = string.Empty;

        [DynamoDBVersion]
        public long? Version { get; set; }
    }
}
