using System.Collections.Generic;

namespace DigitalMakerApi.Requests
{
    public class CreateInstanceRequest
    {
        public CreateInstanceRequest()
        {
            this.InstanceId = string.Empty;
            this.InstanceName = string.Empty;
        }

        public string InstanceId { get; set; }

        public string InstanceName { get; set; }
    }
}
