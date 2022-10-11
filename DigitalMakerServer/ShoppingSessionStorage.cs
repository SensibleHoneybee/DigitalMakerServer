using Amazon.DynamoDBv2.DataModel;

namespace DigitalMakerServer
{
    public class ShoppingSessionStorage
    {
        public ShoppingSessionStorage()
        {
            this.Id = string.Empty;
            this.Content = string.Empty;    
        }

        public string Id { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        public string Content { get; set; }

        public string CheckoutConnectionId { get; set; } = string.Empty;

        public string? CustomerScannerConnectionId { get; set; } = string.Empty;

        [DynamoDBVersion]
        public long? Version { get; set; }
    }
}
