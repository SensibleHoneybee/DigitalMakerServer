using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Requests;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task<List<RootResponse>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger);
    }
}
