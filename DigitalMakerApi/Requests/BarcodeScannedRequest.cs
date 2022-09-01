using System.Collections.Generic;

namespace DigitalMakerApi.Requests
{
    public class BarcodeScannedRequest
    {
        public string InstanceId { get; set; } = string.Empty;

        public string InstanceName { get; set; } = string.Empty;
    }
}
