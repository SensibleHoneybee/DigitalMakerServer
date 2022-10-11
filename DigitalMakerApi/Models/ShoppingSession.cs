namespace DigitalMakerApi.Models
{
    public class ShoppingSession
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string InstanceId { get; set; } = string.Empty;

        public string ShopperName { get; set; } = string.Empty;

        public List<Variable> Variables { get; set; } = new List<Variable>();
    }
}
