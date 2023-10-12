using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using DigitalMakerApi;
using DigitalMakerApi.Models;
using DigitalMakerApi.Requests;
using DigitalMakerApi.Responses;
using DigitalMakerPythonInterface;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

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

        public async Task<List<ResponseWithClientId>> CreateInstanceAsync(CreateInstanceRequest request, string connectionId, ILambdaLogger logger)
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

            var response = new InstanceCreatedResponse();

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ConnectToInstanceAsync(ConnectToInstanceRequest request, string connectionId, ILambdaLogger logger)
        {
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);

            if (instanceStorage == null)
            {
                // No instance yet for this participant.
                var notCreatedResponse = new InstanceDoesNotExistResponse();

                // Response should be sent only to the caller
                return new[] { new ResponseWithClientId(notCreatedResponse, connectionId) }.ToList();
            }

            var possibleInstance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (possibleInstance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            var instance = possibleInstance;

            // And we need to update the instance with the latest connection ID if different
            if (instanceStorage.InstanceAdminConnectionId != connectionId)
            {
                instanceStorage.InstanceAdminConnectionId = connectionId;
                await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);
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

            instance.InputEventHandlers.Add(new InputEventHandler { NameOfEvent = request.InputEventHandlerName, PythonCode = request.PythonCode });

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new FullInstanceResponse { Instance = instance };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> DeleteInputEventHandlerAsync(DeleteInputEventHandlerRequest request, string connectionId, ILambdaLogger logger)
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
                throw new Exception("You have attempted to delete input handler from a screen that is not the instance admin. Please reconnect.");
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

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new FullInstanceResponse { Instance = instance };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> UpdateCodeAsync(UpdateCodeRequest request, string connectionId, ILambdaLogger logger)
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
                throw new Exception("You have attempted to add new input handler from a screen that is not the instance admin. Please reconnect.");
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

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new FullInstanceResponse { Instance = instance };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> StartOrStopRunningAsync(StartOrStopRunningRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("UpdateCodeRequest.InstanceId must be supplied");
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

            instance.IsRunning = request.Run;

            // Update the game in the DB
            instanceStorage.Content = JsonConvert.SerializeObject(instance);

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new FullInstanceResponse { Instance = instance };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ConnectInputOutputDeviceAsync(ConnectInputOutputDeviceRequest request, string connectionId, ILambdaLogger logger)
        {
            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
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

            var response = new FullInstanceResponse { Instance = instance };

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
                var response0a = new UserMessageResponse { InstanceId = request.InstanceId, Message = $"Input event {request.InputName} was called with data '{request.Data}'." };
                var response0b = new UserMessageResponse { InstanceId = request.InstanceId, Message = $"No input event handler found." };

                var response = new NoInputHandlerResponse { InstanceId = request.InstanceId, InputName = request.InputName };
                return new[]
                {
                    new ResponseWithClientId(response0a, instanceStorage.InstanceAdminConnectionId),
                    new ResponseWithClientId(response0b, instanceStorage.InstanceAdminConnectionId),
                    new ResponseWithClientId(response, connectionId)
                }.ToList();
            }

            var pythonScriptProviderLogger = new DigitalMakerServerLogger<PythonScriptProvider>(logger);
            var pythonScriptRunnerLogger = new DigitalMakerServerLogger<IronPythonScriptRunner>(logger);
            var pythonScriptProvider = new PythonScriptProvider(pythonScriptProviderLogger);
            var pythonScriptRunner = new IronPythonScriptRunner(pythonScriptProvider, pythonScriptRunnerLogger);

            var pythonInputData = new PythonInputData
            {
                // No variables in the current version of the system
            };

            var pythonOutputData = await pythonScriptRunner.RunPythonProcessAsync(eventHandler.PythonCode, pythonInputData);

            // Now convert the output requests into messages to send to output devices
            var outputReceiversByName = instance.OutputReceivers
                .GroupBy(x => x.OutputReceiverName)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
            var responsesWithClientId = new List<ResponseWithClientId>();

            // First, notify the instance admin that the input event was called in the first place
            var response1a = new UserMessageResponse { InstanceId = request.InstanceId, Message = $"Input event {request.InputName} was called with data '{request.Data}'" };
            var response1b = new UserMessageResponse { InstanceId = request.InstanceId, Message = "An input event handler was found for this input." };
            responsesWithClientId.Add(new ResponseWithClientId(response1a, instanceStorage.InstanceAdminConnectionId));
            responsesWithClientId.Add(new ResponseWithClientId(response1b, instanceStorage.InstanceAdminConnectionId));

            foreach (var outputAction in pythonOutputData.OutputActions)
            {
                var response3 = new UserMessageResponse { InstanceId = request.InstanceId, Message = $"Output {outputAction.ActionName} was called with data '{data}'" };
                responsesWithClientId.Add(new ResponseWithClientId(response3, instanceStorage.InstanceAdminConnectionId));

                var sanitisedActionName = RemoveWhiteSpace(outputAction.ActionName);
                if (outputReceiversByName.TryGetValue(outputAction.ActionName, out var relevantOutputReceivers))
                {
                    foreach (var relevantOutputReceiver in relevantOutputReceivers)
                    {
                        var data = outputAction.Argument?.ToString() ?? "";
                        var response2 = new OutputActionResponse { InstanceId = request.InstanceId, OutputName = outputAction.ActionName, Data = data };
                        responsesWithClientId.Add(new ResponseWithClientId(response2, relevantOutputReceiver.ConnectionId));

                        // Also notify the instance admin that this was called
                        var response4 = new UserMessageResponse { InstanceId = request.InstanceId, Message = $"Output receiver was found" };
                        responsesWithClientId.Add(new ResponseWithClientId(response3, instanceStorage.InstanceAdminConnectionId));
                    }
                }
            }
         
            return responsesWithClientId;
        }

        private static string RemoveWhiteSpace(string input)
        {
            return Regex.Replace(input, @"\s+", "");
        }
    }
}
