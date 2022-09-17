using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Requests;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task<List<ResponseWithClientId>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger);

        Task<List<ResponseWithClientId>> StartShoppingAsync(StartShoppingRequest request, string connectionId, ILambdaLogger logger);

        ////Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger);
    }
}
