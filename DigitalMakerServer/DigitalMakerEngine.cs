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

        public DigitalMakerEngine(
            IDynamoDBContext instanceTableDDBContext)
        {
            this.instanceTableDDBContext = instanceTableDDBContext;
        }

        public async Task<List<ResponseWithClientId>> GetOrCreateInstanceAsync(GetOrCreateInstanceRequest request, string connectionId, ILambdaLogger logger)
        {
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);

            Instance instance;
            if (instanceStorage == null)
            {
                // No instance yet for this participant. Create one.
                instance = new Instance
                {
                    InstanceId = request.InstanceId,
                    ParticipantNames = request.ParticipantNames
                };

                logger.LogLine($"Created instance bits. ID: {instance.InstanceId}. Name: {instance.ParticipantNames}");

                // And create wrapper to store it in DynamoDB
                instanceStorage = new InstanceStorage
                {
                    Id = request.InstanceId,
                    CreatedTimestamp = DateTime.UtcNow,
                    Content = JsonConvert.SerializeObject(instance),
                    InstanceAdminConnectionId = connectionId
                };

                logger.LogLine($"Saving instance with id {instanceStorage.Id}");
                await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);
            }
            else
            {
                var possibleInstance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
                if (possibleInstance == null)
                {
                    throw new Exception($"Instance {request.InstanceId} has no valid content");
                }

                instance = possibleInstance;

                var needToResave = false;

                // See if participant names have changed
                if (instance.ParticipantNames != request.ParticipantNames)
                {
                    instance.ParticipantNames = request.ParticipantNames;
                    instanceStorage.Content = JsonConvert.SerializeObject(instance);
                    needToResave = true;
                }

                // And we need to update the instance with the latest connection ID if different
                if (instanceStorage.InstanceAdminConnectionId != connectionId)
                {
                    instanceStorage.InstanceAdminConnectionId = connectionId;
                    needToResave = true;
                }

                if (needToResave)
                {
                    await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);
                }
            }

            var response = new FullInstanceResponse { Instance = instance };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> AddNewInputEventHandlerAsync(AddNewInputEventHandlerRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("AddNewInputEventHandlerRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InputEventHandlerName))
            {
                throw new Exception("AddNewInputEventHandlerRequest.InputEventHandlerName must not be empty");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            if (instanceStorage.InstanceAdminConnectionId != connectionId)
            {
                throw new Exception("You have attempted to add new input handler from a screen that is not the instance admin. Please reconnect.");
            }

            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            if (instance.InputEventHandlers.Any(x => string.Equals(x.NameOfEvent, request.InputEventHandlerName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception($"There is already an input event handler with the name {request.InputEventHandlerName}.");
            }

            instance.InputEventHandlers.Add(new InputEventHandler { NameOfEvent = request.InputEventHandlerName });

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new FullInstanceResponse { Instance = instance };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ConnectOutputReceiverAsync(ConnectOutputReceiverRequest request, string connectionId, ILambdaLogger logger)
        {
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            var existings = instance.OutputReceivers.Where(x => x.OutputReceiverName == request.OutputReceiverName).ToList();

            if (existings.Count > 1)
            {
                throw new Exception($"Instance {request.InstanceId} ({instance.ParticipantNames}) has more than one output receiver with name {request.OutputReceiverName}");
            }

            if (existings.Count == 0)
            {
                // New connector
                instance.OutputReceivers.Add(new OutputReceiver
                {
                    OutputReceiverName = request.OutputReceiverName,
                    ConnectionId = connectionId
                });
            }
            else
            {
                var existing = existings.Single();
                existing.ConnectionId = connectionId;
            }

            // And save back the instance
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            logger.LogLine($"Saving instance with id {instance.InstanceId}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new SuccessResponse { Message = $"Successfully connected output {request.OutputReceiverName} to instance {instance.ParticipantNames}" };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InputName))
            {
                throw new Exception("InputReceivedRequest.InputName must be supplied");
            }

            // Now retrieve its instance
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            // Now find the code block for the input received
            var eventHandler = instance.InputEventHandlers.SingleOrDefault(x => string.Equals(x.NameOfEvent, request.InputName, StringComparison.OrdinalIgnoreCase));

            if (eventHandler == null)
            {
                // No input handler is not an error, as the user may not have defined one. Send back a response.
                var response = new NoInputHandlerResponse { InstanceId = request.InstanceId, InputName = request.InputName };
                return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
            }

            var pythonScriptProviderLogger = new DigitalMakerServerLogger<PythonScriptProvider>(logger);
            var pythonScriptRunnerLogger = new DigitalMakerServerLogger<IronPythonScriptRunner>(logger);
            var pythonScriptProvider = new PythonScriptProvider(pythonScriptProviderLogger);
            var pythonScriptRunner = new IronPythonScriptRunner(pythonScriptProvider, pythonScriptRunnerLogger);

            var pythonInputData = new PythonInputData
            {
                // No variables in the current version of the system
            };

            var pythonResults = await pythonScriptRunner.RunPythonProcessAsync(eventHandler.PythonCode, pythonInputData);

            // Now convert the output requests into messages to send to output devices
            throw new NotImplementedException();
        }
    }
}
