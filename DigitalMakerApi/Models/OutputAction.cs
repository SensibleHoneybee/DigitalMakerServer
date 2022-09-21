namespace DigitalMakerApi.Models
{
    public class OutputAction
    {
        public string ActionName { get; set; } = string.Empty;

        public dynamic? Argument { get; set; } = default;
    }
}
