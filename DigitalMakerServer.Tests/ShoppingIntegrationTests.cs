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

        var expectedResponseInner = new ShoppingSessionCreatedResponse
        {
            ShoppingSessionId = innerRequest.ShoppingSessionId
        };

        var expectedResponseOuter = new { ResponseType = expectedResponseInner.ResponseType, Content = JsonConvert.SerializeObject(expectedResponseInner) };

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
                        { new Dictionary<string, AttributeValue>{ {Functions.ConnectionIdField, new AttributeValue { S = connectionId } } } }
                    }
                });
            });

        Func<string, IAmazonApiGatewayManagementApi> apiGatewayFactory = ((endpoint) =>
        {
            Assert.Equal("https://test-domain/test-stage", endpoint);
            return _mockApiGatewayClient.Object;
        });

        _mockApiGatewayClient.Setup(client => client.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PostToConnectionRequest, CancellationToken>((request, token) =>
            {
                var actualMessage = new StreamReader(request.Data).ReadToEnd();
                Assert.Equal(JsonConvert.SerializeObject(expectedResponseOuter), actualMessage);
            });

        var functions = new Functions(_mockDDBClient.Object, apiGatewayFactory, new Mock<IDynamoDBContext>().Object, new Mock<IDynamoDBContext>().Object, tableName);

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
    }
}