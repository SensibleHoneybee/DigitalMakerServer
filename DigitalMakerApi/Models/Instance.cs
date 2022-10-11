﻿using System.Collections.Generic;

namespace DigitalMakerApi.Models
{
    public class Instance
    {
        public string InstanceId { get; set; } = string.Empty;

        public string InstanceName { get; set; } = string.Empty;

        public string PlayerName { get; set; } = string.Empty;

        public string InstanceState { get; set; } = string.Empty;

        public List<Variable> Variables { get; set; } = new List<Variable>();

        public List<InputEventHandler> InputEventHandlers { get; set; } = new List<InputEventHandler>();
    }
}
