using Amazon.ApiGatewayManagementApi;
using Amazon.Lambda.Core;
using DigitalMakerApi;
using Newtonsoft.Json;
using System.Threading.Tasks.Dataflow;

namespace DigitalMakerServer
{
    public class OutboundMessageQueueProcessor
    {
        private readonly AsyncQueue<ResponseWithClientId> queue = new AsyncQueue<ResponseWithClientId>();

        private readonly Func<string, string, IAmazonApiGatewayManagementApi, ILambdaContext, Task> sendMessageToClientFunc;

        private readonly IAmazonApiGatewayManagementApi apiClient;
        
        private readonly ILambdaContext context;

        public OutboundMessageQueueProcessor(
            Func<string, string, IAmazonApiGatewayManagementApi, ILambdaContext, Task> sendMessageToClientFunc, 
            IAmazonApiGatewayManagementApi apiClient, 
            ILambdaContext context)
        {
            this.sendMessageToClientFunc = sendMessageToClientFunc;
            this.apiClient = apiClient;
            this.context = context;
        }

        public int Count => this.queue.Count;

        public async Task Run(CancellationToken cancellationToken)
        {
            await foreach (var responseWithClientId in queue.WithCancellation(cancellationToken))
            {
                var encodedResponse = new ResponseWrapper
                {
                    ResponseType = responseWithClientId.Response.ResponseType,
                    Content = JsonConvert.SerializeObject(responseWithClientId.Response)
                };

                var responseJson = JsonConvert.SerializeObject(encodedResponse);
                context.Logger.LogLine($"Sending JSON response {responseJson} to client {responseWithClientId.ClientId}.");

                await this.sendMessageToClientFunc(responseWithClientId.ClientId, responseJson, this.apiClient, this.context);

                if (responseWithClientId.Response.ResponseType == DigitalMakerResponseType.OutputAction)
                {
                    // Always pause after sending an output, so that user can see results in real time
                    await Task.Delay(1500, cancellationToken);
                }
            }
        }

        public void Enqueue(ResponseWithClientId responseWithClientId)
        {
            this.queue.Enqueue(responseWithClientId);
        }

        private class AsyncQueue<T> : IAsyncEnumerable<T>
        {
            private readonly SemaphoreSlim enumerationSemaphore = new SemaphoreSlim(1);
            private readonly BufferBlock<T> bufferBlock = new BufferBlock<T>();

            public int Count => this.bufferBlock.Count;

            public void Enqueue(T item) =>
                this.bufferBlock.Post(item);

            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default)
            {
                // We lock this so we only ever enumerate once at a time.
                // That way we ensure all items are returned in a continuous
                // fashion with no 'holes' in the data when two foreach compete.
                await this.enumerationSemaphore.WaitAsync();
                try
                {
                    // Return new elements until cancellationToken is triggered.
                    while (true)
                    {
                        // Make sure to throw on cancellation so the Task will transfer into a canceled state
                        token.ThrowIfCancellationRequested();
                        yield return await this.bufferBlock.ReceiveAsync(token);
                    }
                }
                finally
                {
                    this.enumerationSemaphore.Release();
                }
            }
        }
    }
}
