namespace DigitalMakerApi.Requests
{
    public class StartShoppingRequest
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string InstanceId { get; set; } = string.Empty;

        public string ShopperName { get; set; } = string.Empty;
    }
}
