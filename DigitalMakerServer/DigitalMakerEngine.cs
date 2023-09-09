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

        public async Task<List<ResponseWithClientId>> LoginMeetingAdminAsync(LoginMeetingAdminRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingAdminPassword))
            {
                throw new Exception("LoginMeetingAdminRequest.MeetingAdminPassword must be supplied");
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

            var response = new MeetingAdminLoggedInResponse();

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

            var meeting = new Meeting
            {
                MeetingName = request.MeetingName,
                MeetingPasswordHash = this.secretHasher.Hash(request.MeetingPassword),
                IsActive = true
            };

            var meetingStorage = new MeetingStorage
            {
                Id = request.MeetingId,
                Content = JsonConvert.SerializeObject(meeting),
                MeetingAdminConnectionId = connectionId,
                CreatedTimestamp = DateTime.UtcNow
            };

            logger.LogLine($"Created meeting bits. ID: {meetingStorage.Id}. Name: {meeting.MeetingName}");

            logger.LogLine($"Saving meeting with id {meetingStorage.Id}");
            await this.meetingTableDDBContext.SaveAsync<MeetingStorage>(meetingStorage);

            var response = new MeetingOnlyResponse { MeetingId = request.MeetingId, MeetingName = request.MeetingName };

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
                throw new Exception("Meeting password incorrect");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var response = new MeetingOnlyResponse { MeetingId = meetingStorage.Id, MeetingName = meeting.MeetingName };

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
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            if (!this.secretHasher.Verify(request.MeetingPassword, meeting.MeetingPasswordHash))
            {
                throw new Exception("Meeting password incorrect");
            }

            var response = new MeetingOnlyResponse { MeetingId = request.MeetingId, MeetingName = meeting.MeetingName };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> GetParticipantsForMeetingAsync(GetParticipantsForMeetingRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("GetParticipantsForMeetingRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("GetParticipantsForMeetingRequest.MeetingPassword must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            if (!this.secretHasher.Verify(request.MeetingPassword, meeting.MeetingPasswordHash))
            {
                throw new Exception("Meeting password incorrect");
            }

            var participantIdsAndNames = meeting.Participants
                .Select(x => new ParticipantIdAndName { ParticipantId = x.ParticipantId, ParticipantNames = x.ParticipantNames })
                .ToList();

            var response = new ParticipantIdsAndNamesResponse { ParticipantIdsAndNames = participantIdsAndNames };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> JoinNewParticipantAsync(JoinNewParticipantRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("JoinMeetingRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.MeetingPassword))
            {
                throw new Exception("JoinMeetingRequest.MeetingPassword must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("JoinMeetingRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantNames))
            {
                throw new Exception("JoinMeetingRequest.ParticipantNames must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantPassword))
            {
                throw new Exception("JoinMeetingRequest.ParticipantPassword must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("JoinMeetingRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            if (!this.secretHasher.Verify(request.MeetingPassword, meeting.MeetingPasswordHash))
            {
                throw new Exception("Meeting password incorrect");
            }

            meeting.Participants.Add(
                new ParticipantDetails
                {
                    ParticipantId = request.ParticipantId,
                    ParticipantNames = request.ParticipantNames,
                    ParticipantPasswordHash = this.secretHasher.Hash(request.ParticipantPassword),
                    LoginCipher = request.LoginCipher
                });

            meetingStorage.Content = JsonConvert.SerializeObject(meeting);

            // And save it back
            logger.LogLine($"Saving meeting with id {meetingStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<MeetingStorage>(meetingStorage);

            var response = new MeetingWithParticipantResponse
            {
                MeetingId = meetingStorage.Id,
                MeetingName = meeting.MeetingName,
                ParticipantId = request.ParticipantId,
                ParticipantNames = request.ParticipantNames,
                LoginCipher = request.LoginCipher
            };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> RejoinMeetingAndParticipantWithLoginCipherAsync(RejoinMeetingAndParticipantWithLoginCipherRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("RejoinMeetingAndParticipantWithLoginCipherRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("RejoinMeetingAndParticipantWithLoginCipherRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("RejoinMeetingAndParticipantWithLoginCipherRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher incorrect");
            }

            var response = new MeetingWithParticipantResponse
            {
                MeetingId = meetingStorage.Id,
                MeetingName = meeting.MeetingName,
                ParticipantId = participant.ParticipantId,
                ParticipantNames = participant.ParticipantNames,
                LoginCipher = request.LoginCipher,

            };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> RejoinParticipantWithPasswordAsync(RejoinParticipantWithPasswordRequest request, string connectionId, ILambdaLogger logger)
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
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            if (!this.secretHasher.Verify(request.MeetingPassword, meeting.MeetingPasswordHash))
            {
                throw new Exception("Meeting password incorrect");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (!this.secretHasher.Verify(request.ParticipantPassword, participant.ParticipantPasswordHash))
            {
                throw new Exception("Participant password incorrect");
            }

            meetingStorage.Content = JsonConvert.SerializeObject(meeting);

            // And save it back
            logger.LogLine($"Saving meeting with id {meetingStorage.Id}.");
            await this.instanceTableDDBContext.SaveAsync<MeetingStorage>(meetingStorage);

            var response = new MeetingOnlyResponse { MeetingId = meetingStorage.Id, MeetingName = meeting.MeetingName };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }

        public async Task<List<ResponseWithClientId>> GetOrCreateInstanceAsync(GetOrCreateInstanceRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("CreateInstanceRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("CreateInstanceRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("CreateInstanceRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
            }

            // Now locate instanes Find the instance in question
            var config = new DynamoDBOperationConfig { IndexName = "GameCodeIndex" };

            var queryResult = await this.instanceTableDDBContext.QueryAsync<InstanceStorage>(request.ParticipantId, config).GetRemainingAsync();

            Instance instance = null;
            InstanceStorage instanceStorage = null;

            if (queryResult.Count == 0)
            {
                // No instance yet for this participant. Create one.
                var instanceId = Guid.NewGuid().ToString();
                var instanceName = $"Instance for {participant.ParticipantNames}";
                instance = new Instance
                {
                    InstanceId = instanceId,
                    InstanceName = instanceName,
                    InstanceState = InstanceState.NotRunning
                };

                logger.LogLine($"Created instance bits. ID: {instance.InstanceId}. Name: {instance.InstanceName}");

                // And create wrapper to store it in DynamoDB
                instanceStorage = new InstanceStorage
                {
                    Id = instanceId,
                    ParticipantId = request.ParticipantId,
                    CreatedTimestamp = DateTime.UtcNow,
                    Content = JsonConvert.SerializeObject(instance),
                    InstanceAdminConnectionId = connectionId
                };

                logger.LogLine($"Saving instance with id {instanceStorage.Id}");
                await this.instanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);
            }
            else if (queryResult.Count > 1)
            {
                throw new Exception($"More than one instance for participant(s) {participant.ParticipantNames} was found.");
            }
            else
            {
                // Loadaed a single instance.
                var instanceStorageIdHolder = queryResult.Single();

                instanceStorage = await this.instanceTableDDBContext.LoadAsync<InstanceStorage>(instanceStorageIdHolder.Id);
                var possibleInstance = JsonConvert.DeserializeObject<Instance>(instanceStorage.Content);
                if (possibleInstance == null)
                {
                    throw new Exception($"Instance {instanceStorageIdHolder.Id} has no valid content");
                }

                instance = possibleInstance;
            }

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
                throw new Exception("ReconnectInstanceAdminRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("ReconnectInstanceAdminRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("ReconnectInstanceAdminRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
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
                throw new Exception("AddNewInputEventHandlerRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("AddNewInputEventHandlerRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("AddNewInputEventHandlerRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
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

        public async Task<List<ResponseWithClientId>> AddNewVariableAsync(AddNewVariableRequest request, string connectionId, ILambdaLogger logger)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                throw new Exception("AddNewVariableRequest.InstanceId must be supplied");
            }

            if (string.IsNullOrEmpty(request.VariableName))
            {
                throw new Exception("AddNewVariableRequest.VariableName must not be empty");
            }

            if (string.IsNullOrEmpty(request.MeetingId))
            {
                throw new Exception("AddNewVariableRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("AddNewVariableRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("AddNewVariableRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
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

            if (instance.Variables.Any(x => string.Equals(x.Name, request.VariableName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception($"There is already a variable with the name {request.VariableName}.");
            }

            instance.Variables.Add(new Variable { Name = request.VariableName });

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
                throw new Exception("StartShoppingRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("StartShoppingRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("StartShoppingRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
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
                throw new Exception("ConnectCustomerScannerRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("ConnectCustomerScannerRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("ConnectCustomerScannerRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
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
                throw new Exception("ReconnectShoppingSessionRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("ReconnectShoppingSessionRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("ReconnectShoppingSessionRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
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
                throw new Exception("InputReceivedRequest.MeetingId must be supplied");
            }

            if (string.IsNullOrEmpty(request.ParticipantId))
            {
                throw new Exception("InputReceivedRequest.ParticipantId must be supplied");
            }

            if (string.IsNullOrEmpty(request.LoginCipher))
            {
                throw new Exception("InputReceivedRequest.LoginCipher must be supplied");
            }

            var meetingStorage = await this.meetingTableDDBContext.LoadAsync<MeetingStorage>(request.MeetingId);
            var meeting = JsonConvert.DeserializeObject<Meeting>(meetingStorage.Content);
            if (meeting == null)
            {
                throw new Exception($"Meeting {request.MeetingId} has no valid content");
            }

            var participant = meeting.Participants.SingleOrDefault(x => x.ParticipantId == request.ParticipantId);
            if (participant == null)
            {
                throw new Exception($"Meeting {request.MeetingId} does not have a participant with ID {request.ParticipantId}");
            }

            if (request.LoginCipher != participant.LoginCipher)
            {
                throw new Exception("Login cipher is incorrect");
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
