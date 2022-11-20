﻿namespace DigitalMakerApi.Requests
{
    public class StartCheckoutRequest
    {
        public string ShoppingSessionId { get; set; } = string.Empty;

        public string InstanceId { get; set; } = string.Empty;

        public string ShopperName { get; set; } = string.Empty;

        public string MeetingId { get; set; } = string.Empty;

        public string MeetingPassword { get; set; } = string.Empty;
    }
}
