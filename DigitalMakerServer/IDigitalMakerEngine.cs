using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Requests;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task<List<ResponseWithClientId>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ReconnectInstanceAdminAsync(ReconnectInstanceAdminRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> AddNewInputEventHandlerAsync(AddNewInputEventHandlerRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> StartCheckoutAsync(StartCheckoutRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ConnectCustomerScannerAsync(ConnectCustomerScannerRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ReconnectCheckoutAsync(ReconnectCheckoutRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger);
    }
}
