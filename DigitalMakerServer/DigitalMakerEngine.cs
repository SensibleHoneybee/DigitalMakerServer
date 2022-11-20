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
        private const string MeetingAdminSingletonId = "ea3c6398-2731-4ebf-8746-7024716bfba3";

        private readonly IDynamoDBContext meetingAdminTableDDBContext;

        private readonly IDynamoDBContext meetingTableDDBContext;

        private readonly IDynamoDBContext instanceTableDDBContext;

        private readonly IDynamoDBContext shoppingSessionTableDDBContext;

        private readonly ISecretHasher secretHasher;

        public DigitalMakerEngine(
            IDynamoDBContext meetingAdminTableDDBContext,
            IDynamoDBContext meetingTableDDBContext,
            IDynamoDBContext instanceTableDDBContext,
            IDynamoDBContext shoppingSessionTableDDBContext,
            ISecretHasher secretHasher)
        {
            this.meetingAdminTableDDBContext = meetingAdminTableDDBContext;
            this.meetingTableDDBContext = meetingTableDDBContext;
            this.instanceTableDDBContext = instanceTableDDBContext;
            this.shoppingSessionTableDDBContext = shoppingSessionTableDDBContext;
            this.secretHasher = secretHasher;
        }

        public async Task<List<ResponseWithClientId>> ConnectMeetingAdminAsync(ConnectMeetingAdminRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingAdminPassword))
            {
                throw new Exception("ConnectMeetingAdminRequest.MeetingAdminPassword must be supplied");
            }

            var meetingAdminStorage = await this.meetingAdminTableDDBContext.LoadAsync<MeetingAdminStorage>(MeetingAdminSingletonId);
            if (!this.secretHasher.Verify(request.MeetingAdminPassword, meetingAdminStorage.MeetingAdminPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            // Set the new connection ID
            meetingAdminStorage.MeetingAdminConnectionId = connectionId;

            // And save it back
            logger.LogLine($"Saving meeting admin with id {MeetingAdminSingletonId}.");
            await this.meetingAdminTableDDBContext.SaveAsync<MeetingAdminStorage>(meetingAdminStorage);

            var response = new ConnectedMeetingAdminResponse();

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> CreateMeetingAsync(CreateMeetingRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateMeetingRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingName))
            {
                throw new Exception("CreateMeetingRequest.MeetingName must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateMeetingRequest.MeetingPassword must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingAdminPassword))
            {
                throw new Exception("CreateMeetingRequest.MeetingAdminPassword must be supplied");
            }

            var meetingAdminStorage = await this.meetingAdminTableDDBContext.LoadAsync<MeetingAdminStorage>(MeetingAdminSingletonId);

            if (!this.secretHasher.Verify(request.MeetingAdminPassword, meetingAdminStorage.MeetingAdminPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var meetingStorage = new MeetingStorage
            {
                Id = request.MeetingId,
                MeetingName = request.MeetingName,
                MeetingPasswordHash = this.secretHasher.Hash(request.MeetingPassword),
                MeetingAdminConnectionId = connectionId,
                CreatedTimestamp = DateTime.UtcNow
            };

            logger.LogLine($"Created meeting bits. ID: {meetingStorage.Id}. Name: {meetingStorage.MeetingName}");

            logger.LogLine($"Saving meeting with id {meetingStorage.Id}");
            await this.meetingTableDDBContext.SaveAsync<MeetingStorage>(meetingStorage);

            var response = new SuccessResponse { Message = $"Successfully created meeting {request.MeetingName}. Meeting ID: {request.MeetingId}" };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> JoinMeetingAsAdminAsync(JoinMeetingAsAdminRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("JoinMeetingAsAdminRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingAdminPassword))
            {
                throw new Exception("JoinMeetingAsAdminRequest.MeetingAdminPassword must be supplied");
            }

            var meetingAdminStorage = await this.meetingAdminTableDDBContext.LoadAsync<MeetingAdminStorage>(MeetingAdminSingletonId);

            if (!this.secretHasher.Verify(request.MeetingAdminPassword, meetingAdminStorage.MeetingAdminPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);

            var response = new FullMeetingResponse { MeetingId = meetingStorage.Id, MeetingName = meetingStorage.MeetingName };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> JoinMeetingAsync(JoinMeetingRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("JoinMeetingRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("JoinMeetingRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);

            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var response = new FullMeetingResponse { MeetingId = meetingStorage.Id, MeetingName = meetingStorage.MeetingName };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
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

            if (string.IsNullOrEmpty(request.PlayerName))
            {
                throw new Exception("CreateInstanceRequest.PlayerName must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateInstanceRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var instance = new Instance
            {
                InstanceId = request.InstanceId,
                InstanceName = request.InstanceName,
                PlayerName = request.PlayerName,
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

            var response = new FullInstanceResponse { Instance = instance };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ReconnectInstanceAdminAsync(ReconnectInstanceAdminRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("ReconnectInstanceAdminRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateInstanceRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            // Set the new connection ID
            instanceStorage.InstanceAdminConnectionId = connectionId;

            // And save it back
            logger.LogLine($"Saving instance with id {instanceStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

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

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateInstanceRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
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

        public async Task<List<ResponseWithClientId>> StartCheckoutAsync(StartCheckoutRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ShoppingSessionId))
            {
                throw new Exception("StartShoppingRequest.ShoppingSessionId must be supplied");
            }

            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("StartShoppingRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateInstanceRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(request.InstanceId);
            if (instanceStorage == null || string.IsNullOrEmpty(instanceStorage.InstanceAdminConnectionId))
            {
                throw new Exception($"Null InstanceStorage or empty connection ID for {request.InstanceId}");
            }

            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {request.InstanceId} has no valid content");
            }

            var shoppingSession = new ShoppingSession
            {
                ShoppingSessionId = request.ShoppingSessionId,
                InstanceId = request.InstanceId,
                ShopperName = request.ShopperName,
                Variables = instance.Variables
            };

            logger.LogLine($"Created shopping session. ID: {shoppingSession.ShoppingSessionId}. Instance: {shoppingSession.InstanceId}");

            // And create wrapper to store it in DynamoDB
            var shoppingSessionStorage = new ShoppingSessionStorage
            {
                Id = request.InstanceId,
                CreatedTimestamp = DateTime.UtcNow,
                CheckoutConnectionId = connectionId,
                CustomerScannerConnectionId = null,
                Content = JsonConvert.SerializeObject(shoppingSession)
            };

            logger.LogLine($"Saving shopping session with id {shoppingSession.ShoppingSessionId}");
            await this.shoppingSessionTableDDBContext.SaveAsync<ShoppingSessionStorage>(shoppingSessionStorage);

            var response = new FullShoppingSessionResponse { Instance = instance, ShoppingSession = shoppingSession };

            // Response should be sent to the caller and also to the instance admin
            return new[] {
                new ResponseWithClientId(response, connectionId),
                new ResponseWithClientId(response, instanceStorage.InstanceAdminConnectionId)
            }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ConnectCustomerScannerAsync(ConnectCustomerScannerRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ShoppingSessionId))
            {
                throw new Exception("ConnectCustomerScannerRequest.ShoppingSessionId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateInstanceRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var shoppingSessionStorage = await this.shoppingSessionTableDDBContext.LoadAsync<ShoppingSessionStorage>(request.ShoppingSessionId);
            var shoppingSession = JsonConvert.DeserializeObject<ShoppingSession>(shoppingSessionStorage.Content);
            if (shoppingSession == null)
            {
                throw new Exception($"Shopping session {request.ShoppingSessionId} has no valid content");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(shoppingSession.InstanceId);
            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {shoppingSession.InstanceId} of shopping session {request.ShoppingSessionId} has no valid content");
            }

            // Set the new customer scanner connection ID
            shoppingSessionStorage.CustomerScannerConnectionId = connectionId;

            // And save it back
            logger.LogLine($"Saving shopping session with id {shoppingSessionStorage.Id}.");
            await this.shoppingSessionTableDDBContext.SaveAsync<ShoppingSessionStorage>(shoppingSessionStorage);

            var response = new FullShoppingSessionResponse { Instance = instance, ShoppingSession = shoppingSession };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> ReconnectCheckoutAsync(ReconnectCheckoutRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.ShoppingSessionId))
            {
                throw new Exception("ReconnectShoppingSessionRequest.ShoppingSessionId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateInstanceRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
            }

            var shoppingSessionStorage = await this.shoppingSessionTableDDBContext.LoadAsync<ShoppingSessionStorage>(request.ShoppingSessionId);
            var shoppingSession = JsonConvert.DeserializeObject<ShoppingSession>(shoppingSessionStorage.Content);
            if (shoppingSession == null)
            {
                throw new Exception($"Shopping session {request.ShoppingSessionId} has no valid content");
            }

            var instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(shoppingSession.InstanceId);
            var instance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
            if (instance == null)
            {
                throw new Exception($"Instance {shoppingSession.InstanceId} of shopping session {request.ShoppingSessionId} has no valid content");
            }

            // Set the new connection ID
            shoppingSessionStorage.CheckoutConnectionId = connectionId;

            // And save it back
            logger.LogLine($"Saving shopping session with id {shoppingSessionStorage.Id}.");
            await this.shoppingSessionTableDDBContext.SaveAsync<ShoppingSessionStorage>(shoppingSessionStorage);

            var response = new FullShoppingSessionResponse { Instance = instance, ShoppingSession = shoppingSession };

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

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("CreateInstanceRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            if (!this.secretHasher.Verify(request.MeetingPassword, meetingStorage.MeetingPasswordHash))
            {
                throw new Exception("Meeting Admin password incorrect");
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
