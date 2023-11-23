﻿namespace DigitalMakerApi.Models
{
    public class Instance
    {
        public string InstanceId { get; set; } = string.Empty;

        public string ParticipantNames { get; set; } = string.Empty;

        public List<InputEventHandler> InputEventHandlers { get; set; } = new List<InputEventHandler>();

        public List<OutputReceiver> OutputReceivers { get; set; } = new List<OutputReceiver>();

        public string VersionIdentifier { get; set; } = string.Empty;
    }
}
