using Amazon.Lambda.Core;
using DigitalMakerApi.Requests;
using DigitalMakerApi.Responses;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task<List<ResponseWithClientId>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger);
    }
}
