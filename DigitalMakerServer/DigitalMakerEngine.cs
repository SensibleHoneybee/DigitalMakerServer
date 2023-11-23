using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Models;
using DigitalMakerApi.Requests;
using DigitalMakerApi.Responses;
using DigitalMakerPythonInterface;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DigitalMakerServer
{
    public class DigitalMakerEngine : IDigitalMakerEngine
    {
        private readonly IDynamoDBContext instanceTableDDBContext;

        private readonly IWordGenerator wordGenerator;

        public DigitalMakerEngine(IDynamoDBContext instanceTableDDBContext, IWordGenerator wordGenerator)
        {
            this.instanceTableDDBContext = instanceTableDDBContext;
            this.wordGenerator = wordGenerator;
        }

        public async Task CreateInstanceAsync(CreateInstanceRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
        {
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);

            if (instanceStorage != null)
            {
                throw new Exception($"Instance {request.InstanceId} already exists");
            }

            var instance = new Instance
            {
                InstanceId = request.InstanceId,
                ParticipantNames = request.ParticipantNames
            };

            logger.LogLine($"Created instance bits. ID: {instance.InstanceId}. Name: {instance.ParticipantNames}");

            instance.VersionIdentifier = this.wordGenerator.GetWords();

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

            // Response should be sent only to the caller
            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(
                    new InstanceCreatedResponse(),
                    connectionId));
        }

        public async Task ConnectToInstanceAsync(ConnectToInstanceRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
        {
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            if (instanceStorage == null)
            {
                // No instance yet for this participant.
                // Response should be sent only to the caller
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(new InstanceDoesNotExistResponse(), connectionId));
                return;
            }

            var possibleInstance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (possibleInstance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            var instance = possibleInstance;

            instance.VersionIdentifier = this.wordGenerator.GetWords();
            instanceStorage.Content = JsonConvert.SerializeObject(instance);
            instanceStorage.InstanceAdminConnectionId = connectionId;
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            // Response should be sent to the caller and all client consoles
            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, connectionId));

            foreach (var outputReceiver in instance.OutputReceivers)
            {
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, outputReceiver.ConnectionId));
            }
        }

        public async Task AddNewInputEventHandlerAsync(AddNewInputEventHandlerRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
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
                throw new Exception("You have attempted to add new input handler from a screen that is not the instance admin. Please reconnect by refreshing your browser.");
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

            instance.InputEventHandlers.Add(new InputEventHandler { NameOfEvent = request.InputEventHandlerName, PythonCode = request.PythonCode });

            instance.VersionIdentifier = this.wordGenerator.GetWords();

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            // Response should be sent to the caller and all client consoles
            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, connectionId));

            foreach (var outputReceiver in instance.OutputReceivers)
            {
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, outputReceiver.ConnectionId));
            }
        }

        public async Task DeleteInputEventHandlerAsync(DeleteInputEventHandlerRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("DeleteInputEventHandlerRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InputEventHandlerName))
            {
                throw new Exception("DeleteInputEventHandlerRequest.InputEventHandlerName must not be empty");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            if (instanceStorage.InstanceAdminConnectionId != connectionId)
            {
                throw new Exception("You have attempted to delete input handler from a screen that is not the instance admin. Please reconnect by refreshing your browser.");
            }

            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            if (!instance.InputEventHandlers.Any(x => string.Equals(x.NameOfEvent, request.InputEventHandlerName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception($"There is no input event handler with the name {request.InputEventHandlerName}.");
            }

            var handlerToRemove = instance.InputEventHandlers.SingleOrDefault(x => x.NameOfEvent == request.InputEventHandlerName);
            if (handlerToRemove != null)
            {
                instance.InputEventHandlers.Remove(handlerToRemove);
            }

            instance.VersionIdentifier = this.wordGenerator.GetWords();

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            // Response should be sent to the caller and all client consoles
            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, connectionId));

            foreach (var outputReceiver in instance.OutputReceivers)
            {
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, outputReceiver.ConnectionId));
            }
        }

        public async Task UpdateCodeAsync(UpdateCodeRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("UpdateCodeRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InputEventHandlerName))
            {
                throw new Exception("UpdateCodeRequest.InputEventHandlerName must not be empty");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            if (instanceStorage.InstanceAdminConnectionId != connectionId)
            {
                throw new Exception("You have attempted to update code from a screen that is not the instance admin. Please reconnect by refreshing your browser.");
            }

            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            var inputEventHandler = instance.InputEventHandlers.SingleOrDefault(x => string.Equals(x.NameOfEvent, request.InputEventHandlerName, StringComparison.OrdinalIgnoreCase));
            if (inputEventHandler == null)
            {
                throw new Exception($"There is no input event handler with the name {request.InputEventHandlerName}.");
            }

            inputEventHandler.PythonCode = request.PythonCode;

            instance.VersionIdentifier = this.wordGenerator.GetWords();

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            // Response should be sent to the caller and all client consoles
            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, connectionId));

            foreach (var outputReceiver in instance.OutputReceivers)
            {
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, outputReceiver.ConnectionId));
            }
        }

        public async Task ConnectInputOutputDeviceAsync(ConnectInputOutputDeviceRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
        {
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            if (instanceStorage == null)
            {
                // No instance yet for this participant.
                // Response should be sent only to the caller
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(new InstanceDoesNotExistResponse(), connectionId));
                return;
            }

            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            foreach (var outputReceiverName in request.OutputReceiverNames)
            {
                var existings = instance.OutputReceivers.Where(x => x.OutputReceiverName == outputReceiverName).ToList();

                if (existings.Count > 1)
                {
                    throw new Exception($"Instance {request.InstanceId} ({instance.ParticipantNames}) has more than one output receiver with name {outputReceiverName}");
                }

                if (existings.Count == 0)
                {
                    // New connector

                    instance.OutputReceivers.Add(new OutputReceiver
                    {
                        OutputReceiverName = outputReceiverName,
                        ConnectionId = connectionId
                    });
                }
                else
                {
                    var existing = existings.Single();
                    existing.ConnectionId = connectionId;
                }
            }

            // And save back the instance
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            logger.LogLine($"Saving instance with id {instance.InstanceId}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            // Response should be sent only to the caller
            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(new FullInstanceResponse { Instance = instance }, connectionId));
        }

        public async Task HandleInputReceivedAsync(InputReceivedRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
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
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(
                        new UserMessageResponse { InstanceId = request.InstanceId, Message = $"{DateTime.Now.ToString("HH:mm:ss")}: Input event {request.InputName} was called with data '{request.Data}'. No input event handler found." },
                        instanceStorage.InstanceAdminConnectionId));
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(
                        new NoInputHandlerResponse { InstanceId = request.InstanceId, InputName = request.InputName },
                        connectionId));
                return;
            }

            var pythonScriptProviderLogger = new DigitalMakerServerLogger<PythonScriptProvider>(logger);
            var pythonScriptRunnerLogger = new DigitalMakerServerLogger<IronPythonScriptRunner>(logger);
            var pythonScriptProvider = new PythonScriptProvider(pythonScriptProviderLogger);
            var pythonScriptRunner = new IronPythonScriptRunner(pythonScriptProvider, pythonScriptRunnerLogger);

            var pythonInputData = new PythonInputData
            {
                // No variables in the current version of the system
                Variables = new List<Variable>
                {
                    new Variable { Name = "data", Value = request.Data }
                }
            };

            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(
                    new UserMessageResponse { InstanceId = request.InstanceId, Message = $"{DateTime.Now.ToString("HH:mm:ss")}: Input event '{request.InputName}' was called with data '{request.Data}'. Running input handler code now." },
                    instanceStorage.InstanceAdminConnectionId));

            var pythonOutputData = await pythonScriptRunner.RunPythonProcessAsync(eventHandler.PythonCode, pythonInputData);

            // Now convert the output requests into messages to send to output devices
            var outputReceiversByName = instance.OutputReceivers
                .GroupBy(x => x.OutputReceiverName)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var outputAction in pythonOutputData.OutputActions)
            {
                outboundMessageQueueProcessor.Enqueue(
                    new ResponseWithClientId(
                        new UserMessageResponse { InstanceId = request.InstanceId, Message = $"{DateTime.Now.ToString("HH:mm:ss")}: Output {outputAction.ActionName} was called with data '{outputAction.Argument}'." },
                        instanceStorage.InstanceAdminConnectionId));

                var sanitisedActionName = RemoveWhiteSpace(outputAction.ActionName);
                if (outputReceiversByName.TryGetValue(outputAction.ActionName, out var relevantOutputReceivers))
                {
                    foreach (var relevantOutputReceiver in relevantOutputReceivers)
                    {
                        var data = outputAction.Argument?.ToString() ?? "";
                        outboundMessageQueueProcessor.Enqueue(
                            new ResponseWithClientId(
                                new OutputActionResponse { InstanceId = request.InstanceId, OutputName = outputAction.ActionName, Data = data },
                                relevantOutputReceiver.ConnectionId));
                    }
                }
            }

            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(
                    new UserMessageResponse { InstanceId = request.InstanceId, Message = $"{DateTime.Now.ToString("HH:mm:ss")}: Finished running input event handler." },
                    instanceStorage.InstanceAdminConnectionId));
        }

        public void HandleConnectionTestNumber(ConnectionTestNumberRequest request, string connectionId, OutboundMessageQueueProcessor outboundMessageQueueProcessor, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ConnectionTestNumber))
            {
                throw new Exception("ConnectionTestNumberRequest.ConnectionTestNumber must be supplied");
            }

            // No need to do very much - just echo the same number back to the caller to prove that we're here
            outboundMessageQueueProcessor.Enqueue(
                new ResponseWithClientId(
                    new ConnectionTestNumberResponse { ConnectionTestNumber = request.ConnectionTestNumber },
                    connectionId));
        }

        private static string RemoveWhiteSpace(string input)
        {
            return Regex.Replace(input, @"\s+", "");
        }
    }
}
