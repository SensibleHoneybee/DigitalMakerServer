﻿using System.Collections.Generic;

namespace DigitalMakerApi.Models
{
    public class Instance
    {
        public string InstanceId { get; set; } = string.Empty;

        public string InstanceName { get; set; } = string.Empty;

        /// <summary>
        /// public string InstanceCode { get; set; }
        /// </summary>

        public string InstanceState { get; set; } = string.Empty;
    }
}
