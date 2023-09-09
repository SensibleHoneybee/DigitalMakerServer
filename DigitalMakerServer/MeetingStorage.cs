using Amazon.DynamoDBv2.DataModel;

namespace DigitalMakerServer
{
    public class MeetingStorage
    {
        public MeetingStorage()
        {
            this.Id = string.Empty;
            this.Content = string.Empty;
        }

        public string Id { get; set; }

        ////[DynamoDBGlobalSecondaryIndexHashKey("InstanceIndex")]
        ////public string InstanceCode { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        public string Content { get; set; }

        public string MeetingAdminConnectionId { get; set; } = string.Empty;

        [DynamoDBVersion]
        public long? Version { get; set; }
    }
}
