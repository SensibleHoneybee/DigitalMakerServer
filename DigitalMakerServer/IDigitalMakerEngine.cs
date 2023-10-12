using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Requests;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task<List<ResponseWithClientId>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ConnectToInstanceAsync(ConnectToInstanceRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> AddNewInputEventHandlerAsync(AddNewInputEventHandlerRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> DeleteInputEventHandlerAsync(DeleteInputEventHandlerRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> UpdateCodeAsync(UpdateCodeRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> StartOrStopRunningAsync(StartOrStopRunningRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ConnectInputOutputDeviceAsync(ConnectInputOutputDeviceRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger);
    }
}
