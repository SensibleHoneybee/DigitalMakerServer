using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using DigitalMakerApi.Models;
using DigitalMakerApi.Requests;
using DigitalMakerApi.Responses;
using Newtonsoft.Json;

namespace DigitalMakerServer
{
    public class DigitalMakerEngine : IDigitalMakerEngine
    {
        public DigitalMakerEngine(IDynamoDBContext instanceTableDDBContext)
        {
            this.InstanceTableDDBContext = instanceTableDDBContext;
        }

        IDynamoDBContext InstanceTableDDBContext { get; }

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
                Id = request.InstanceId,
                InstanceName = request.InstanceName,
                ////InstanceCode = instanceCode,
                InstanceState = InstanceState.NotRunning
            };

            logger.LogLine($"Created instance bits. ID: {instance.Id}. Name: {instance.InstanceName}");

            // And create wrapper to store it in DynamoDB
            var instanceStorage = new InstanceStorage
            {
                Id = request.InstanceId,
                ////GameCode = instanceCode,
                CreatedTimestamp = DateTime.UtcNow,
                Content = JsonConvert.SerializeObject(instance)
            };

            logger.LogLine($"Saving instance with id {instanceStorage.Id}");
            await this.InstanceTableDDBContext.SaveAsync<InstanceStorage>(instanceStorage);

            var response = new InstanceCreatedResponse { InstanceId = request.InstanceId };

            // Response should be sent only to the caller
            return new[] { new ResponseWithClientId(response, connectionId) }.ToList();
        }
    }
}
