namespace DigitalMakerApi.Models
{
    public class ShoppingSession
    {
        public string ShoppingSessionId { get; set; }

        public string InstanceId { get; set; }

        public List<Variable> Variables { get; set; } = new List<Variable>();
    }
}
