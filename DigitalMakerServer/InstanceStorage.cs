using Amazon.DynamoDBv2.DataModel;

namespace DigitalMakerServer
{
    public class InstanceStorage
    {
        public InstanceStorage()
        {
            this.Id = string.Empty;
            this.Content = string.Empty;
        }

        public string Id { get; set; }

        ////[DynamoDBGlobalSecondaryIndexHashKey("InstanceIndex")]
        ////public string InstanceCode { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        public string Content { get; set; }

        [DynamoDBVersion]
        public long? Version { get; set; }
    }
}
