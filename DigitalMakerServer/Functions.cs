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


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace DigitalMakerServer;

public class Functions
{
    public const string ConnectionIdField = "connectionId";
    private const string CONNECTION_TABLE_NAME_ENV = "CONNECTION_TABLE_NAME";
    private const string INSTANCE_TABLE_NAME_ENV = "InstanceTable";
    private const string SHOPPING_SESSION_TABLE_NAME_ENV = "ShoppingSessionTable";

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
    /// DynamoDB context for the storage and retrieval of shopping session objects to the database
    /// </summary>
    IDynamoDBContext ShoppingSessionTableDDBContext { get; }

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

        // Now check to see if an shopping sessions table name was passed in through environment variables and if so 
        // add the table mapping.
        var shoppingSessionTableName = System.Environment.GetEnvironmentVariable(SHOPPING_SESSION_TABLE_NAME_ENV);
        if (!string.IsNullOrEmpty(shoppingSessionTableName))
        {
            AWSConfigsDynamoDB.Context.TypeMappings[typeof(ShoppingSessionStorage)] = new Amazon.Util.TypeMapping(typeof(ShoppingSessionStorage), shoppingSessionTableName);
        }
        this.ShoppingSessionTableDDBContext = new DynamoDBContext(this.DDBClient, config);

        // New up a digital maker engine for use in the lifetime of this running
        this.DigitalMakerEngine = new DigitalMakerEngine(this.InstanceTableDDBContext, this.ShoppingSessionTableDDBContext);
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
        IDynamoDBContext shoppingSessionTableDDBContext,
        string connectionMappingTable)
    {
        this.DDBClient = ddbClient;
        this.ApiGatewayManagementApiClientFactory = apiGatewayManagementApiClientFactory;
        this.InstanceTableDDBContext = instanceTableDDBContext;
        this.ShoppingSessionTableDDBContext = shoppingSessionTableDDBContext;
        this.ConnectionMappingTable = connectionMappingTable;
        this.DigitalMakerEngine = new DigitalMakerEngine(this.InstanceTableDDBContext, this.ShoppingSessionTableDDBContext);
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

                List<ResponseWithClientId> responsesWithClientIds;
                switch (requestWrapper.RequestType)
                {
                    case RequestType.CreateInstance:
                        var createInstanceRequest = JsonConvert.DeserializeObject<CreateInstanceRequest>(requestWrapper.Content);
                        if (createInstanceRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid CreateInstanceRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        responsesWithClientIds = await this.DigitalMakerEngine.CreateInstanceAsync(createInstanceRequest, connectionId, context.Logger);
                        break;
                    case RequestType.InputReceived:
                        var inputReceivedRequest = JsonConvert.DeserializeObject<InputReceivedRequest>(requestWrapper.Content);
                        if (inputReceivedRequest == null)
                        {
                            context.Logger.LogLine("Root request content was not a valid InputReceivedRequest");
                            return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest };
                        }
                        responsesWithClientIds = await this.DigitalMakerEngine.HandleInputReceivedAsync(inputReceivedRequest, connectionId, context.Logger);
                        break;
                    default:
                        throw new Exception($"Unknown message request type: {requestWrapper.RequestType}");
                }

                context.Logger.LogLine($"Game responses: {responsesWithClientIds.Count}");

                // List all of the current connections. In a more advanced use case the table could be used to grab a group of connection ids for a chat group.
                var scanRequest = new ScanRequest
                {
                    TableName = ConnectionMappingTable,
                    ProjectionExpression = ConnectionIdField
                };

                var scanResponse = await DDBClient.ScanAsync(scanRequest);

                // Construct the IAmazonApiGatewayManagementApi which will be used to send the message to.
                var apiClient = ApiGatewayManagementApiClientFactory(endpoint);

                var responsesByClientId = responsesWithClientIds.GroupBy(x => x.ClientId).ToDictionary(x => x.Key, x => x.Select(x => x.Response).ToList());

                // Loop through all of the connections and broadcast the message out to the connections.
                var count = 0;
                foreach (var item in scanResponse.Items)
                {
                    var connectedClientConnectionId = item[ConnectionIdField].S;

                    List<IResponse> responses;
                    if (!responsesByClientId.TryGetValue(connectedClientConnectionId, out responses))
                    {
                        // This connection isn't amongst those to receive a message response.
                        continue;
                    }

                    foreach (var response in responses)
                    {
                        var encodedResponse = new { ResponseType = response.ResponseType, Content = JsonConvert.SerializeObject(response) };
                        var responseJson = JsonConvert.SerializeObject(encodedResponse);
                        context.Logger.LogLine($"Sending JSON response {responseJson} to client {connectedClientConnectionId}.");

                        await SendMessageToClient(connectedClientConnectionId, responseJson, apiClient, context);

                        count++;
                    }
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = "Data sent to " + count + " connection" + (count == 1 ? "" : "s")
                };
            }
            catch (Exception e1)
            {
                var apiClient = ApiGatewayManagementApiClientFactory(endpoint);
                var errorResponse = new ErrorResponse { Message = e1.Message };
                var encodedResponse = new { ResponseType = errorResponse.ResponseType, Content = JsonConvert.SerializeObject(errorResponse) };
                var responseJson = JsonConvert.SerializeObject(encodedResponse);
                await SendMessageToClient(connectionId, responseJson, apiClient, context);
                throw;
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