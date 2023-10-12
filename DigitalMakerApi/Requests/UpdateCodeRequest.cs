namespace DigitalMakerApi.Requests
{
    public class UpdateCodeRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public string InputEventHandlerName { get; set; } = string.Empty;

        public string PythonCode { get; set; } = string.Empty;
    }
}
