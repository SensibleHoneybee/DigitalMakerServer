using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Requests;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task<List<ResponseWithClientId>> LoginMeetingAdminAsync(LoginMeetingAdminRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> CreateMeetingAsync(CreateMeetingRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> JoinMeetingAsAdminAsync(JoinMeetingAsAdminRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> JoinMeetingAsync(JoinMeetingRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> GetParticipantsForMeetingAsync(GetParticipantsForMeetingRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> JoinNewParticipantAsync(JoinNewParticipantRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> RejoinMeetingAndParticipantWithLoginCipherAsync(RejoinMeetingAndParticipantWithLoginCipherRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> RejoinParticipantWithPasswordAsync(RejoinParticipantWithPasswordRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> GetOrCreateInstanceAsync(GetOrCreateInstanceRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ReconnectInstanceAdminAsync(ReconnectInstanceAdminRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> AddNewInputEventHandlerAsync(AddNewInputEventHandlerRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> AddNewVariableAsync(AddNewVariableRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> StartCheckoutAsync(StartCheckoutRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ConnectCustomerScannerAsync(ConnectCustomerScannerRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ReconnectCheckoutAsync(ReconnectCheckoutRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger);
    }
}
