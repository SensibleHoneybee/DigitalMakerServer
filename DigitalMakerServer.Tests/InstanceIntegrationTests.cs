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
using DigitalMakerApi.Models;

namespace DigitalMakerServer.Tests;

public class InstanceIntegrationTests
{
    public InstanceIntegrationTests()
    {
    }

    [Fact]
    public async Task TestCreateInstance()
    {
        Mock<IAmazonDynamoDB> _mockDDBClient = new Mock<IAmazonDynamoDB>();
        Mock<IAmazonApiGatewayManagementApi> _mockApiGatewayClient = new Mock<IAmazonApiGatewayManagementApi>();
        string tableName = "mocktable";
        string connectionId = "test-id";
        string instanceId = "Test instance ID";
        string participantNames = "Test participant Names";

        var innerRequest = new CreateInstanceRequest
        {
            InstanceId = instanceId,
            ParticipantNames = participantNames
        };

        var outerRequest = new RequestWrapper
        {
            RequestType = RequestType.CreateInstance,
            Content = JsonConvert.SerializeObject(innerRequest)
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
                        { new Dictionary<string, AttributeValue>{ {Functions.ConnectionIdField, new AttributeValue { S = connectionId } } } }
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

        // Brand new instance, so load returns null
        var instanceTableDynamoDBContext = new Mock<IDynamoDBContext>();
        instanceTableDynamoDBContext
            .Setup(x => x.LoadAsync<InstanceStorage?>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((InstanceStorage?)null));

        InstanceStorage? outputInstanceStorage = null;
        instanceTableDynamoDBContext
            .Setup(x => x.SaveAsync<InstanceStorage>(It.IsAny<InstanceStorage>(), It.IsAny<CancellationToken>()))
            .Callback<InstanceStorage, CancellationToken>((ss, ct) => outputInstanceStorage = ss);

        var secretHasher = new Mock<ISecretHasher>();

        var functions = new Functions(
            _mockDDBClient.Object,
            apiGatewayFactory,
            instanceTableDynamoDBContext.Object,
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

        // Check that the saved instance matches expected data
        Assert.NotNull(outputInstanceStorage);
        Assert.Equal(connectionId, outputInstanceStorage.InstanceAdminConnectionId);

        // And check that the appropriate messages were sent to the caller
        Assert.Single(messagesReceived);
        var messageReceived = messagesReceived.Single();
        Assert.Equal(connectionId, messageReceived.ConnectionId);

        var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(messageReceived.Response);
        Assert.NotNull(responseWrapper);
        Assert.Equal(DigitalMakerResponseType.InstanceCreated, responseWrapper.ResponseType);

        var messageResponse = JsonConvert.DeserializeObject<InstanceCreatedResponse>(responseWrapper.Content);
        Assert.NotNull(messageResponse);
    }

    [Fact]
    public async Task TestConnectToInstanceExistingNewConnectionId()
    {
        Mock<IAmazonDynamoDB> _mockDDBClient = new Mock<IAmazonDynamoDB>();
        Mock<IAmazonApiGatewayManagementApi> _mockApiGatewayClient = new Mock<IAmazonApiGatewayManagementApi>();
        string tableName = "mocktable";
        string connectionId = "test-id";
        string newConnectionId = "test-id-new";
        string instanceId = "Test instance ID";
        string participantNames = "Test participant Names";

        var innerRequest = new ConnectToInstanceRequest
        {
            InstanceId = instanceId
        };

        var outerRequest = new RequestWrapper
        {
            RequestType = RequestType.ConnectToInstance,
            Content = JsonConvert.SerializeObject(innerRequest)
        };

        var instanceStorage = new InstanceStorage
        {
            InstanceAdminConnectionId = connectionId,
            Content = JsonConvert.SerializeObject(new Instance { InstanceId = instanceId, ParticipantNames = participantNames })
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
                        { new Dictionary<string, AttributeValue>{ {Functions.ConnectionIdField, new AttributeValue { S = newConnectionId } } } }
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

        // Brand new instance, so load returns null
        var instanceTableDynamoDBContext = new Mock<IDynamoDBContext>();
        instanceTableDynamoDBContext
            .Setup(x => x.LoadAsync<InstanceStorage>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(instanceStorage));

        InstanceStorage? outputInstanceStorage = null;
        instanceTableDynamoDBContext
            .Setup(x => x.SaveAsync<InstanceStorage>(It.IsAny<InstanceStorage>(), It.IsAny<CancellationToken>()))
            .Callback<InstanceStorage, CancellationToken>((ss, ct) => outputInstanceStorage = ss);

        var secretHasher = new Mock<ISecretHasher>();

        var functions = new Functions(
            _mockDDBClient.Object,
            apiGatewayFactory,
            instanceTableDynamoDBContext.Object,
            tableName);

        var lambdaContext = new TestLambdaContext();

        var request = new APIGatewayProxyRequest
        {
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ConnectionId = newConnectionId,
                DomainName = "test-domain",
                Stage = "test-stage"
            },
            Body = "{\"message\":\"sendmessage\", \"data\":" + JsonConvert.SerializeObject(message) + "}"
        };

        var response = await functions.SendMessageHandler(request, lambdaContext);

        Assert.Equal(200, response.StatusCode);

        // Check that the saved instance matches expected data
        Assert.NotNull(outputInstanceStorage);
        Assert.Equal(newConnectionId, outputInstanceStorage.InstanceAdminConnectionId);

        // And check that the appropriate messages were sent to the caller
        Assert.Single(messagesReceived);
        var messageReceived = messagesReceived.Single();
        Assert.Equal(newConnectionId, messageReceived.ConnectionId);

        var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(messageReceived.Response);
        Assert.NotNull(responseWrapper);
        Assert.Equal(DigitalMakerResponseType.FullInstance, responseWrapper.ResponseType);

        var messageResponse = JsonConvert.DeserializeObject<FullInstanceResponse>(responseWrapper.Content);
        Assert.NotNull(messageResponse);
        Assert.Equal(instanceId, messageResponse.Instance.InstanceId);
    }

    [Fact]
    public async Task TestConnectToInstanceExistingSameConnectionId()
    {
        Mock<IAmazonDynamoDB> _mockDDBClient = new Mock<IAmazonDynamoDB>();
        Mock<IAmazonApiGatewayManagementApi> _mockApiGatewayClient = new Mock<IAmazonApiGatewayManagementApi>();
        string tableName = "mocktable";
        string connectionId = "test-id";
        string instanceId = "Test instance ID";
        string participantNames = "Test participant Names";

        var innerRequest = new ConnectToInstanceRequest
        {
            InstanceId = instanceId
        };

        var outerRequest = new RequestWrapper
        {
            RequestType = RequestType.ConnectToInstance,
            Content = JsonConvert.SerializeObject(innerRequest)
        };

        var instanceStorage = new InstanceStorage
        {
            InstanceAdminConnectionId = connectionId,
            Content = JsonConvert.SerializeObject(new Instance { InstanceId = instanceId, ParticipantNames = participantNames })
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
                        { new Dictionary<string, AttributeValue>{ {Functions.ConnectionIdField, new AttributeValue { S = connectionId } } } }
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

        // Brand new instance, so load returns null
        var instanceTableDynamoDBContext = new Mock<IDynamoDBContext>();
        instanceTableDynamoDBContext
            .Setup(x => x.LoadAsync<InstanceStorage>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(instanceStorage));

        InstanceStorage? outputInstanceStorage = null;
        instanceTableDynamoDBContext
            .Setup(x => x.SaveAsync<InstanceStorage>(It.IsAny<InstanceStorage>(), It.IsAny<CancellationToken>()))
            .Callback<InstanceStorage, CancellationToken>((ss, ct) => outputInstanceStorage = ss);

        var secretHasher = new Mock<ISecretHasher>();

        var functions = new Functions(
            _mockDDBClient.Object,
            apiGatewayFactory,
            instanceTableDynamoDBContext.Object,
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

        // Check that there was no saved instance
        Assert.Null(outputInstanceStorage);

        // And check that the appropriate messages were sent to the caller
        Assert.Single(messagesReceived);
        var messageReceived = messagesReceived.Single();
        Assert.Equal(connectionId, messageReceived.ConnectionId);

        var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(messageReceived.Response);
        Assert.NotNull(responseWrapper);
        Assert.Equal(DigitalMakerResponseType.FullInstance, responseWrapper.ResponseType);

        var messageResponse = JsonConvert.DeserializeObject<FullInstanceResponse>(responseWrapper.Content);
        Assert.NotNull(messageResponse);
        Assert.Equal(instanceId, messageResponse.Instance.InstanceId);
        Assert.Equal(participantNames, messageResponse.Instance.ParticipantNames);
    }

    public async Task TestConnectToInstanceDoesNotExist()
    {
        Mock<IAmazonDynamoDB> _mockDDBClient = new Mock<IAmazonDynamoDB>();
        Mock<IAmazonApiGatewayManagementApi> _mockApiGatewayClient = new Mock<IAmazonApiGatewayManagementApi>();
        string tableName = "mocktable";
        string connectionId = "test-id";
        string instanceId = "Test instance ID";

        var innerRequest = new ConnectToInstanceRequest
        {
            InstanceId = instanceId
        };

        var outerRequest = new RequestWrapper
        {
            RequestType = RequestType.ConnectToInstance,
            Content = JsonConvert.SerializeObject(innerRequest)
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
                        { new Dictionary<string, AttributeValue>{ {Functions.ConnectionIdField, new AttributeValue { S = connectionId } } } }
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

        // Brand new instance, so load returns null
        var instanceTableDynamoDBContext = new Mock<IDynamoDBContext>();
        instanceTableDynamoDBContext
            .Setup(x => x.LoadAsync<InstanceStorage?>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult((InstanceStorage?)null));

        InstanceStorage? outputInstanceStorage = null;
        instanceTableDynamoDBContext
            .Setup(x => x.SaveAsync<InstanceStorage>(It.IsAny<InstanceStorage>(), It.IsAny<CancellationToken>()))
            .Callback<InstanceStorage, CancellationToken>((ss, ct) => outputInstanceStorage = ss);

        var secretHasher = new Mock<ISecretHasher>();

        var functions = new Functions(
            _mockDDBClient.Object,
            apiGatewayFactory,
            instanceTableDynamoDBContext.Object,
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

        // Check that there was no saved instance
        Assert.Null(outputInstanceStorage);

        // And check that the appropriate messages were sent to the caller
        Assert.Single(messagesReceived);
        var messageReceived = messagesReceived.Single();
        Assert.Equal(connectionId, messageReceived.ConnectionId);

        var responseWrapper = JsonConvert.DeserializeObject<ResponseWrapper>(messageReceived.Response);
        Assert.NotNull(responseWrapper);
        Assert.Equal(DigitalMakerResponseType.InstanceDoesNotExist, responseWrapper.ResponseType);

        var messageResponse = JsonConvert.DeserializeObject<InstanceDoesNotExistResponse>(responseWrapper.Content);
        Assert.NotNull(messageResponse);
    }

    private record ResponseDetails(string Response, string ConnectionId);
}