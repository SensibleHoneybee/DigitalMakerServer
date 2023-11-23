using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Amazon.Runtime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2.DataModel;
using Amazon;
using Newtonsoft.Json;
using DigitalMakerApi;
using DigitalMakerApi.Requests;
using DigitalMakerApi.Responses;
using System.Collections.Concurrent;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DigitalMakerServer;

public class Functions
{
    public const string ConnectionIdField = "connectionId";
    private const string CONNECTION_TABLE_NAME_ENV = "CONNECTION_TABLE_NAME";
    private const string INSTANCE_TABLE_NAME_ENV = "InstanceTable";

    /// <summary>
    /// DynamoDB table used to store the open connection ids. More advanced use cases could store logged on user map to their connection id to implement direct message chatting.
    /// </summary>
    string ConnectionMappingTable { get; }

    /// <summary>
    /// DynamoDB service client used to store and retieve connection information from the ConnectionMappingTable
    /// </summary>
    IAmazonDynamoDB DDBClient { get; }

    /// <summary>
    /// Factory func to create the AmazonApiGatewayManagementApiClient. This is needed to created per endpoint of the a connection. It is a factory to make it easy for tests
    /// to moq the creation.
    /// </summary>
    Func<string, IAmazonApiGatewayManagementApi> ApiGatewayManagementApiClientFactory { get; }


    /// <summary>
    /// DynamoDB context for the storage and retrieval of instance objects to the database
    /// </summary>
    IDynamoDBContext InstanceTableDDBContext { get; }


    /// <summary>
    /// Engine to perform the calculations and operations
    /// </summary>
    IDigitalMakerEngine DigitalMakerEngine { get; }

    /// <summary>
    /// Default constructor that Lambda will invoke.
    /// </summary>
    public Functions()
    {
        this.DDBClient = new AmazonDynamoDBClient();

        // Grab the name of the connection DynamoDB table from the environment variable setup in the CloudFormation template serverless.template
        if (Environment.GetEnvironmentVariable(CONNECTION_TABLE_NAME_ENV) == null)
        {
            throw new ArgumentException($"Missing required connection table environment variable {CONNECTION_TABLE_NAME_ENV}");
        }

        this.ConnectionMappingTable = Environment.GetEnvironmentVariable(CONNECTION_TABLE_NAME_ENV) ?? "";

        this.ApiGatewayManagementApiClientFactory = (Func<string, AmazonApiGatewayManagementApiClient>)((endpoint) => 
        {
            return new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
            {
                ServiceURL = endpoint
            });
        });

        var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };

        // Now check to see if an instances table name was passed in through environment variables and if so 
        // add the table mapping.
        var instanceTableName = System.Environment.GetEnvironmentVariable(INSTANCE_TABLE_NAME_ENV);
        if (!string.IsNullOrEmpty(instanceTableName))
        {
            AWSConfigsDynamoDB.Context.TypeMappings[typeof(InstanceStorage)] = new Amazon.Util.TypeMapping(typeof(InstanceStorage), instanceTableName);
        }
        this.InstanceTableDDBContext = new DynamoDBContext(this.DDBClient, config);

        // New up a digital maker engine for use in the lifetime of this running
        this.DigitalMakerEngine = new DigitalMakerEngine(
            this.InstanceTableDDBContext,
            new WordGenerator());
    }

    /// <summary>
    /// Constructor used for testing allow tests to pass in moq versions of the service clients.
    /// </summary>
    /// <param name="ddbClient"></param>
    /// <param name="apiGatewayManagementApiClientFactory"></param>
    /// <param name="connectionMappingTable"></param>
    public Functions(IAmazonDynamoDB ddbClient,
        Func<string, IAmazonApiGatewayManagementApi> apiGatewayManagementApiClientFactory,
        IDynamoDBContext instanceTableDDBContext,
        string connectionMappingTable)
    {
        this.DDBClient = ddbClient;
        this.ApiGatewayManagementApiClientFactory = apiGatewayManagementApiClientFactory;
        this.InstanceTableDDBContext = instanceTableDDBContext;
        this.ConnectionMappingTable = connectionMappingTable;
        this.DigitalMakerEngine = new DigitalMakerEngine(this.InstanceTableDDBContext, new WordGenerator());
    }

    public async Task<APIGatewayProxyResponse> OnConnectHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var connectionId = request.RequestContext.ConnectionId;
            context.Logger.LogInformation($"ConnectionId: {connectionId}");

            var ddbRequest = new PutItemRequest
            {
                TableName = ConnectionMappingTable,
                Item = new Dictionary<string, AttributeValue>
                {
                    {ConnectionIdField, new AttributeValue{ S = connectionId}}
                }
            };

            await DDBClient.PutItemAsync(ddbRequest);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = "Connected."
            };
        }
        catch (Exception e)
        {
            context.Logger.LogInformation("Error connecting: " + e.Message);
            context.Logger.LogInformation(e.StackTrace);
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = $"Failed to connect: {e.Message}"
            };
        }
    }

    public async Task<APIGatewayProxyResponse> SendMessageHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Construct the API Gateway endpoint that incoming message will be broadcasted to.
            var domainName = request.RequestContext.DomainName;
            var stage = request.RequestContext.Stage;
            var endpoint = $"https://{domainName}/{stage}";
            context.Logger.LogLine($"API Gateway management endpoint: {endpoint}");

            var connectionId = request.RequestContext.ConnectionId;
            context.Logger.LogLine($"ConnectionId: {connectionId}");

            // Construct the IAmazonApiGatewayManagementApi which will be used to send the message to.
            var apiClient = ApiGatewayManagementApiClientFactory(endpoint);
            var outboundMessageQueueProcessor = new OutboundMessageQueueProcessor(this.SendMessageToClient, apiClient, context);
            var messageQueueCancellationTokenSource = new CancellationTokenSource();
            var outboundMessageQueueProcessorTask = outboundMessageQueueProcessor.Run(messageQueueCancellationTokenSource.Token);

            try
            {
                JsonDocument message = JsonDocument.Parse(request.Body);

                // Grab the data from the JSON body which is the message to broadcasted.
                // As defined in the CloudWatch serverless template, the route is "sendmessage". Thus
                // The body will look something like this: {"message":"sendmessage", "data":"What are you doing?"}
                JsonElement dataProperty;
                if (!message.RootElement.TryGetProperty("data", out dataProperty))
                {
                    context.Logger.LogLine("Failed to find data element in JSON document");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest
                    };
                }

                var data = dataProperty.GetString();
                if (string.IsNullOrWhiteSpace(data))
                {
                    context.Logger.LogLine("JSON data element has no content");
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                }
                context.Logger.LogLine($"JSON Data: {data}");

                var requestWrapper = JsonConvert.DeserializeObject<RequestWrapper>(data);
                if (requestWrapper == null || string.IsNullOrEmpty(requestWrapper.RequestType) || string.IsNullOrEmpty(requestWrapper.Content))
                {
                    context.Logger.LogLine("JSON data element did not contain a valid request, with a type and content");
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                }

                context.Logger.LogLine($"Send Message Request. Type: {requestWrapper.RequestType}. Content: {requestWrapper.Content}");

                // List all of the current connections. In a more advanced use case the table could be used to grab a group of connection ids for a chat group.
                var scanRequest = new ScanRequest
                {
                    TableName = ConnectionMappingTable,
                    ProjectionExpression = ConnectionIdField
                };

                var scanResponse = await DDBClient.ScanAsync(scanRequest);

                switch (requestWrapper.RequestType)
                {
                    case RequestType.CreateInstance:
                        var createInstanceRequest = JsonConvert.DeserializeObject<CreateInstanceRequest>(requestWrapper.Content);
                        if (createInstanceRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid CreateInstanceRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        await TryMultipleTimesAsync(() => this.DigitalMakerEngine.CreateInstanceAsync(createInstanceRequest, connectionId, outboundMessageQueueProcessor, context.Logger));
                        break;
                    case RequestType.ConnectToInstance:
                        var connectToInstanceRequest = JsonConvert.DeserializeObject<ConnectToInstanceRequest>(requestWrapper.Content);
                        if (connectToInstanceRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid ConnectToInstanceRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        await TryMultipleTimesAsync(() => this.DigitalMakerEngine.ConnectToInstanceAsync(connectToInstanceRequest, connectionId, outboundMessageQueueProcessor, context.Logger));
                        break;
                    case RequestType.AddNewInputEventHandler:
                        var addNewInputEventHandlerRequest = JsonConvert.DeserializeObject<AddNewInputEventHandlerRequest>(requestWrapper.Content);
                        if (addNewInputEventHandlerRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid AddNewInputEventHandlerRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        await TryMultipleTimesAsync(() => this.DigitalMakerEngine.AddNewInputEventHandlerAsync(addNewInputEventHandlerRequest, connectionId, outboundMessageQueueProcessor, context.Logger));
                        break;
                    case RequestType.DeleteInputEventHandler:
                        var deleteInputEventHandlerRequest = JsonConvert.DeserializeObject<DeleteInputEventHandlerRequest>(requestWrapper.Content);
                        if (deleteInputEventHandlerRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid DeleteInputEventHandlerRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        await TryMultipleTimesAsync(() => this.DigitalMakerEngine.DeleteInputEventHandlerAsync(deleteInputEventHandlerRequest, connectionId, outboundMessageQueueProcessor, context.Logger));
                        break;
                    case RequestType.UpdateCode:
                        var updateCodeRequest = JsonConvert.DeserializeObject<UpdateCodeRequest>(requestWrapper.Content);
                        if (updateCodeRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid UpdateCodeRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        await TryMultipleTimesAsync(() => this.DigitalMakerEngine.UpdateCodeAsync(updateCodeRequest, connectionId, outboundMessageQueueProcessor, context.Logger));
                        break;
                    case RequestType.ConnectInputOutputDevice:
                        var connectInputOutputDeviceRequest = JsonConvert.DeserializeObject<ConnectInputOutputDeviceRequest>(requestWrapper.Content);
                        if (connectInputOutputDeviceRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid ConnectInputOutputDeviceRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        await TryMultipleTimesAsync(() => this.DigitalMakerEngine.ConnectInputOutputDeviceAsync(connectInputOutputDeviceRequest, connectionId, outboundMessageQueueProcessor, context.Logger));
                        break;
                    case RequestType.InputReceived:
                        var inputReceivedRequest = JsonConvert.DeserializeObject<InputReceivedRequest>(requestWrapper.Content);
                        if (inputReceivedRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid InputReceivedRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        await this.DigitalMakerEngine.HandleInputReceivedAsync(inputReceivedRequest, connectionId, outboundMessageQueueProcessor, context.Logger);
                        break;
                    case RequestType.ConnectionTestNumber:
                        var connectionTestNumberRequest = JsonConvert.DeserializeObject<ConnectionTestNumberRequest>(requestWrapper.Content);
                        if (connectionTestNumberRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid ConnectionTestNumberRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        this.DigitalMakerEngine.HandleConnectionTestNumber(connectionTestNumberRequest, connectionId, outboundMessageQueueProcessor, context.Logger);
                        break;
                    default:
                        throw new Exception($"Unknown message request type: {requestWrapper.RequestType}");
                }

                // Wait till all responses are complete
                while (true)
                {
                    if (outboundMessageQueueProcessor.Count == 0)
                    {
                        break;
                    }

                    await Task.Delay(100);
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception e1)
            {
                var errorResponse = new ErrorResponse { Message = e1.Message + "\r\n" + e1.StackTrace };
                var encodedResponse = new { ResponseType = errorResponse.ResponseType, Content = JsonConvert.SerializeObject(errorResponse) };
                var responseJson = JsonConvert.SerializeObject(encodedResponse);
                await SendMessageToClient(connectionId, responseJson, apiClient, context);
                throw;
            }
            finally
            {
                messageQueueCancellationTokenSource.Cancel();
                await outboundMessageQueueProcessorTask;
            }
        }
        catch (Exception e)
        {
            context.Logger.LogLine("Error in send message handler: " + e.Message);
            context.Logger.LogLine(e.StackTrace);
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Failed to send message: {e.Message}"
            };
        }
    }

    public async Task<APIGatewayProxyResponse> OnDisconnectHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var connectionId = request.RequestContext.ConnectionId;
            context.Logger.LogInformation($"ConnectionId: {connectionId}");

            var ddbRequest = new DeleteItemRequest
            {
                TableName = ConnectionMappingTable,
                Key = new Dictionary<string, AttributeValue>
                {
                    {ConnectionIdField, new AttributeValue {S = connectionId}}
                }
            };

            await DDBClient.DeleteItemAsync(ddbRequest);

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body = "Disconnected."
            };
        }
        catch (Exception e)
        {
            context.Logger.LogInformation("Error disconnecting: " + e.Message);
            context.Logger.LogInformation(e.StackTrace);
            return new APIGatewayProxyResponse
            {
                StatusCode = 500,
                Body = $"Failed to disconnect: {e.Message}"
            };
        }
    }

    private static async Task TryMultipleTimesAsync(Func<Task> funcToTryAsync)
    {
        var failureCounter = 0;
        while (failureCounter < 10)
        {
            try
            {
                await funcToTryAsync();
                return;
            }
            catch (ConditionalCheckFailedException)
            {
                Thread.Sleep(200);
                failureCounter++;
            }
        }

        throw new InvalidOperationException("Conditional check failed 10 times");
    }

    private async Task SendMessageToClient(string connectionId, string responseJson, IAmazonApiGatewayManagementApi apiClient, ILambdaContext context)
    {
        var stream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(responseJson));

        var postConnectionRequest = new PostToConnectionRequest
        {
            ConnectionId = connectionId,
            Data = stream
        };

        try
        {
            context.Logger.LogLine($"Post to connection: {postConnectionRequest.ConnectionId}");
            stream.Position = 0;
            await apiClient.PostToConnectionAsync(postConnectionRequest);
        }
        catch (AmazonServiceException e)
        {
            // API Gateway returns a status of 410 GONE then the connection is no
            // longer available. If this happens, delete the identifier
            // from our DynamoDB table.
            if (e.StatusCode == HttpStatusCode.Gone)
            {
                var ddbDeleteRequest = new DeleteItemRequest
                {
                    TableName = ConnectionMappingTable,
                    Key = new Dictionary<string, AttributeValue>
                                {
                                    {ConnectionIdField, new AttributeValue {S = postConnectionRequest.ConnectionId}}
                                }
                };

                context.Logger.LogLine($"Deleting gone connection: {postConnectionRequest.ConnectionId}");
                await DDBClient.DeleteItemAsync(ddbDeleteRequest);
            }
            else
            {
                context.Logger.LogLine($"Error posting message to {postConnectionRequest.ConnectionId}: {e.Message}");
                context.Logger.LogLine(e.StackTrace);
            }
        }
    }
}