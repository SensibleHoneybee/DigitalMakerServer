using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Models;
using DigitalMakerApi.Requests;
using DigitalMakerApi.Responses;
using DigitalMakerPythonInterface;
using Newtonsoft.Json;

namespace DigitalMakerServer
{
    public class DigitalMakerEngine : IDigitalMakerEngine
    {
        private readonly IDynamoDBContext instanceTableDDBContext;

        private readonly IDynamoDBContext shoppingSessionTableDDBContext;

        public DigitalMakerEngine(
            IDynamoDBContext instanceTableDDBContext,
            IDynamoDBContext shoppingSessionTableDDBContext)
        {
            this.instanceTableDDBContext = instanceTableDDBContext;
            this.shoppingSessionTableDDBContext = shoppingSessionTableDDBContext;
        }

        public async Task<List<ResponseWithClientId>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("CreateInstanceRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InstanceName))
            {
                throw new Exception("CreateInstanceRequest.InstanceName must be supplied");
            }

            // Get a unique ID and code for this instance
            ////var secondsSinceY2K = (long)DateTime.UtcNow.Subtract(new DateTime(2000, 1, 1)).TotalSeconds;
            ////var InstanceCode = CreateInstanceCode(secondsSinceY2K);

            var instance = new Instance
            {
                InstanceId = request.InstanceId,
                InstanceName = request.InstanceName,
                InstanceState = InstanceState.NotRunning
            };

            logger.LogLine($"Created instance bits. ID: {instance.InstanceId}. Name: {instance.InstanceName}");

            // And create wrapper to store it in DynamoDB
            var instanceStorage = new InstanceStorage
            {
                Id = request.InstanceId,
                CreatedTimestamp = DateTime.UtcNow,
                Content = JsonConvert.SerializeObject(instance),
                InstanceAdminConnectionId = connectionId
            };

            logger.LogLine($"Saving instance with id {instanceStorage.Id}");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new InstanceCreatedResponse { InstanceId = request.InstanceId };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ReconnectInstanceAdminAsync(ReconnectInstanceAdminRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("ReconnectInstanceAdminRequest.InstanceId must be supplied");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);

            // Set the new connection ID
            instanceStorage.InstanceAdminConnectionId = connectionId;

            // And save it back
            logger.LogLine($"Saving isntance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new InstanceAdminReconnectedResponse { InstanceId = request.InstanceId };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> StartShoppingAsync(StartShoppingRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ShoppingSessionId))
            {
                throw new Exception("StartShoppingRequest.ShoppingSessionId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("StartShoppingRequest.InstanceId must be supplied");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);

            if (instanceStorage == null || string.IsNullOrEmpty(instanceStorage.InstanceAdminConnectionId))
            {
                throw new Exception($"Null InstanceStorage or empty connection ID for {request.InstanceId}");
            }

            var shoppingSession = new ShoppingSession
            {
                ShoppingSessionId = request.ShoppingSessionId,
                InstanceId = request.InstanceId
            };

            logger.LogLine($"Created shopping session. ID: {shoppingSession.ShoppingSessionId}. Instance: {shoppingSession.InstanceId}");

            // And create wrapper to store it in DynamoDB
            var shoppingSessionStorage = new ShoppingSessionStorage
            {
                Id = request.InstanceId,
                CreatedTimestamp = DateTime.UtcNow,
                ShoppingSessionConnectionId = connectionId,
                Content = JsonConvert.SerializeObject(shoppingSession)
            };

            logger.LogLine($"Saving shopping session with id {shoppingSession.ShoppingSessionId}");
            await this.shoppingSessionTableDDBContext.SaveAsync<ShoppingSessionStorage>(shoppingSessionStorage);

            var response = new ShoppingSessionCreatedResponse { ShoppingSessionId = request.ShoppingSessionId };

            // Response should be sent to the caller and also to the instance admin
            return new[] {
                new ResponseWithClientId(response, connectionId),
                new ResponseWithClientId(response, instanceStorage.InstanceAdminConnectionId)
            }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ReconnectShoppingSessionAsync(ReconnectShoppingSessionRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ShoppingSessionId))
            {
                throw new Exception("ReconnectShoppingSessionRequest.ShoppingSessionId must be supplied");
            }

            var shoppingSessionStorage = await this.instanceTableDDBContext.LoadAsync<ShoppingSessionStorage>(request.ShoppingSessionId);

            // Set the new connection ID
            shoppingSessionStorage.ShoppingSessionConnectionId = connectionId;

            // And save it back
            logger.LogLine($"Saving shopping session with id {shoppingSessionStorage.Id}.");
            await this.shoppingSessionTableDDBContext.SaveAsync<ShoppingSessionStorage>(shoppingSessionStorage);

            var response = new ShoppingSessionReconnectedResponse { InstanceId = request.ShoppingSessionId };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ShoppingSessionId))
            {
                throw new Exception("InputReceivedRequest.ShoppingSessionId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InputName))
            {
                throw new Exception("InputReceivedRequest.InputName must be supplied");
            }

            // Find the shopping session in question
            var shoppingSessionStorage = await this.shoppingSessionTableDDBContext.LoadAsync<ShoppingSessionStorage>(request.ShoppingSessionId);
            var shoppingSession = JsonConvert.DeserializeObject<ShoppingSession>(shoppingSessionStorage.Content);
            if (shoppingSession == null)
            {
                throw new Exception($"Shopping session {request.ShoppingSessionId} has no valid content");
            }

            // Now retrieve its instance
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(shoppingSession.InstanceId);
            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {shoppingSession.InstanceId} for shopping session {request.ShoppingSessionId} has no valid content");
            }

            // Now find the code block for the input received
            var eventHandler = instance.InputEventHandlers.SingleOrDefault(x => string.Equals(x.NameOfEvent, request.InputName, StringComparison.OrdinalIgnoreCase));

            if (eventHandler == null)
            {
                // No input handler is not an error, as the user may not have defined one. Send back a response.
                var response = new NoInputHandlerResponse { ShoppingSessionId = request.ShoppingSessionId, InputName = request.InputName };
                return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
            }

            var pythonScriptProviderLogger = new DigitalMakerServerLogger<PythonScriptProvider>(logger);
            var pythonScriptRunnerLogger = new DigitalMakerServerLogger<IronPythonScriptRunner>(logger);
            var pythonScriptProvider = new PythonScriptProvider(pythonScriptProviderLogger);
            var pythonScriptRunner = new IronPythonScriptRunner(pythonScriptProvider, pythonScriptRunnerLogger);

            var pythonInputData = new PythonInputData
            {
                Variables = instance.Variables
            };

            var pythonResults = await pythonScriptRunner.RunPythonProcessAsync(eventHandler.PythonCode, pythonInputData);

            // Update the variables back again
            shoppingSession.Variables = pythonResults.Variables;

            // Update the variables in the storage
            shoppingSessionStorage.Content = JsonConvert.SerializeObject(shoppingSession);

            // And save it back to the DB
            logger.LogLine($"Saving shopping session with id {shoppingSessionStorage.Id}.");
            await this.shoppingSessionTableDDBContext.SaveAsync<ShoppingSessionStorage>(shoppingSessionStorage);

            // Now convert the output requests into messages to send to output devices
            throw new NotImplementedException();
        }
    }
}
