namespace DigitalMakerApi.Models
{
    public class Variable
    {
        public string Name { get; set; } = string.Empty;

        public string VariableType { get; set; } = string.Empty;

        public dynamic Value { get; set; } = string.Empty;
    }
}
