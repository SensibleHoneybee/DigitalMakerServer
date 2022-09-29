using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;

using Moq;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2.DataModel;
using DigitalMakerApi.Requests;
using DigitalMakerApi;
using Newtonsoft.Json;
using DigitalMakerApi.Responses;

namespace DigitalMakerServer.Tests;

public class ShoppingIntegrationTests
{
    public ShoppingIntegrationTests()
    {
    }

    [Fact]
    public async Task TestStartShopping()
    {
        Mock<IAmazonDynamoDB> _mockDDBClient = new Mock<IAmazonDynamoDB>();
        Mock<IAmazonApiGatewayManagementApi> _mockApiGatewayClient = new Mock<IAmazonApiGatewayManagementApi>();
        string tableName = "mocktable";
        string connectionId = "test-id";
        string instanceAdminConnectionId = "instance-admin-test-id";

        var innerRequest = new StartShoppingRequest
        {
            ShoppingSessionId = "Test session ID",
            InstanceId = "Test instance ID",
            ShopperName = "Test shopper name"
        };

        var outerRequest = new RequestWrapper
        {
            RequestType = RequestType.StartShopping,
            Content = JsonConvert.SerializeObject(innerRequest)
        };

        var instanceStorage = new InstanceStorage
        {
            InstanceAdminConnectionId = instanceAdminConnectionId
        };

        var message = JsonConvert.SerializeObject(outerRequest);

        _mockDDBClient.Setup(client => client.ScanAsync(It.IsAny<ScanRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ScanRequest, CancellationToken>((request, token) =>
            {
                Assert.Equal(tableName, request.TableName);
                Assert.Equal(Functions.ConnectionIdField, request.ProjectionExpression);
            })
            .Returns((ScanRequest r, CancellationToken token) =>
            {
                return Task.FromResult(new ScanResponse
                {
                    Items = new List<Dictionary<string, AttributeValue>>
                    {
                        { new Dictionary<string, AttributeValue>{ {Functions.ConnectionIdField, new AttributeValue { S = connectionId } } } },
                        { new Dictionary<string, AttributeValue>{ {Functions.ConnectionIdField, new AttributeValue { S = instanceAdminConnectionId } } } }
                    }
                });
            });

        Func<string, IAmazonApiGatewayManagementApi> apiGatewayFactory = ((endpoint) =>
        {
            Assert.Equal("https://test-domain/test-stage", endpoint);
            return _mockApiGatewayClient.Object;
        });

        var messagesReceived = new List<ResponseDetails>();
        _mockApiGatewayClient.Setup(client => client.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PostToConnectionRequest, CancellationToken>((request, token) =>
            {
                var actualMessage = new StreamReader(request.Data).ReadToEnd();
                messagesReceived.Add(new ResponseDetails(actualMessage, request.ConnectionId));
            });

        var instanceTableDynamoDBContext = new Mock<IDynamoDBContext>();
        instanceTableDynamoDBContext
            .Setup(x => x.LoadAsync<InstanceStorage>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(instanceStorage));

        ShoppingSessionStorage? outputShoppingSessionStorage = null;
        var shoppingSessionTableDynamoDBContext = new Mock<IDynamoDBContext>();
        shoppingSessionTableDynamoDBContext
            .Setup(x => x.SaveAsync<ShoppingSessionStorage>(It.IsAny<ShoppingSessionStorage>(), It.IsAny<CancellationToken>()))
            .Callback<ShoppingSessionStorage, CancellationToken>((ss, ct) => outputShoppingSessionStorage = ss);

        var functions = new Functions(
            _mockDDBClient.Object,
            apiGatewayFactory,
            instanceTableDynamoDBContext.Object,
            shoppingSessionTableDynamoDBContext.Object,
            tableName);

        var lambdaContext = new TestLambdaContext();

        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ConnectionId = connectionId,
                DomainName = "test-domain",
                Stage = "test-stage"
            },
            Body = "{\"message\":\"sendmessage\", \"data\":" + JsonConvert.SerializeObject(message) + "}"
        };

        var response = await functions.SendMessageHandler(request, lambdaContext);

        Assert.Equal(200, response.StatusCode);

        // Check that the saved shopping session matches expected data
        Assert.NotNull(outputShoppingSessionStorage);
        Assert.Equal(connectionId, outputShoppingSessionStorage.ShoppingSessionConnectionId);

        // And check that the appropriate messages were sent to both the caller and to the instance admin
        Assert.Equal(2, messagesReceived.Count);
        foreach (var testMessage in new[] { Tuple.Create(0, connectionId), Tuple.Create(1, instanceAdminConnectionId) })
        {
            var responseDetails = messagesReceived[testMessage.Item1];
            Assert.Equal(testMessage.Item2, responseDetails.ConnectionId);
            var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(responseDetails.Response);
            Assert.NotNull(responseWrapper);
            Assert.Equal(DigitalMakerResponseType.ShoppingSessionCreated, responseWrapper.ResponseType);
            var messageResponse = JsonConvert.DeserializeObject<ShoppingSessionCreatedResponse>(responseWrapper.Content);
            Assert.NotNull(messageResponse);
            Assert.Equal(innerRequest.ShoppingSessionId, messageResponse.ShoppingSessionId);
        }
    }
    
    private record ResponseDetails(string Response, string ConnectionId);
}