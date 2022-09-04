namespace DigitalMakerApi.Models
{
    public class OutputAction
    {
        public string ActionName { get; set; } = string.Empty;

        public List<dynamic> Arguments { get; set; } = new List<dynamic>();
    }
}
