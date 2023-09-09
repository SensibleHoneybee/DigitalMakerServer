namespace DigitalMakerApi
{
    public static class RequestType
    {
        public const string LoginMeetingAdmin = "LoginMeetingAdmin";

        public const string CreateMeeting = "CreateMeeting";

        public const string JoinMeetingAsAdmin = "JoinMeetingAsAdmin";

        public const string JoinMeeting = "JoinMeeting";

        public const string GetParticipantsForMeeting = "GetParticipantsForMeeting";

        public const string JoinNewParticipant = "JoinNewParticipant";

        public const string RejoinMeetingAndParticipantWithLoginCipher = "RejoinMeetingAndParticipantWithLoginCipher";

        public const string RejoinParticipantWithPassword = "RejoinParticipantWithPassword";

        public const string GetOrCreateInstance = "GetOrCreateInstance";

        public const string ReconnectInstanceAdmin = "ReconnectInstanceAdmin";

        public const string AddNewInputEventHandler = "AddNewInputEventHandler";

        public const string AddNewVariable = "AddNewVariable";

        public const string StartCheckout = "StartCheckout";

        public const string ReconnectCheckout = "ReconnectCheckout";

        public const string ConnectCustomerScanner = "ConnectCustomerScanner";

        public const string InputReceived = "InputReceived";
    }
}
