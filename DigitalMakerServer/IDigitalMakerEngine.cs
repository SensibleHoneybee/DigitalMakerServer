using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Requests;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task<List<ResponseWithClientId>> GetOrCreateInstanceAsync(GetOrCreateInstanceRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> AddNewInputEventHandlerAsync(AddNewInputEventHandlerRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> ConnectOutputReceiverAsync(ConnectOutputReceiverRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger);
    }
}
