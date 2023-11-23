using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Requests;

namespace DigitalMakerServer
{
    public interface IDigitalMakerEngine
    {
        Task CreateInstanceAsync(CreateInstanceRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);

        Task ConnectToInstanceAsync(ConnectToInstanceRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);

        Task AddNewInputEventHandlerAsync(AddNewInputEventHandlerRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);

        Task DeleteInputEventHandlerAsync(DeleteInputEventHandlerRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);

        Task UpdateCodeAsync(UpdateCodeRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);

        Task ConnectInputOutputDeviceAsync(ConnectInputOutputDeviceRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);

        Task HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);

        void HandleConnectionTestNumber(ConnectionTestNumberRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger);
    }
}
