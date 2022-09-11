using DigitalMakerApi.Models;
using DigitalMakerWorkerApp.PythonInterface;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMakerWorkerApp.Tests
{

    public class PythonScriptRunnerTests
    {
        [Fact]
        public async Task ThatSubstitutesPythonScriptAndVariablesAndCallsGateway()
        {
            const string ScriptTemplate = "Foo\r\n{{{VARIABLE_DEFINITIONS}}}\r\n{{{USER_CODE}}}\r\nBar";
            const string UserScript = "if 5 > 2:\r\n  print(\"Five is greater than two!\")";
            const string VariableDefinition = "fish = \"chips\"\r\nmushy_peas = 32.4";

            var pythonScriptExecuted = string.Empty;

            var mockPythonScriptProvider = new Mock<IPythonScriptProvider>();
            mockPythonScriptProvider.Setup(x => x.GetPythonScript()).Returns(ScriptTemplate);

            var mockPythonScriptGateway = new Mock<IPythonScriptGateway>();
            mockPythonScriptGateway
                .Setup(x => x.RunPythonProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((x, y) => pythonScriptExecuted = x);

            var mockPythonVariableDefinitionProvider = new Mock<IPythonVariableDefinitionProvider>();
            mockPythonVariableDefinitionProvider
                .Setup(x => x.GetPythonVariableDefinition(It.IsAny<Variable>()))
                .Returns(VariableDefinition);

            var mockPythonResultParser = new Mock<IPythonResultParser>();

            var mockLogger = new Mock<ILogger<PythonScriptRunner>>();

            var pythonScriptRunner = new PythonScriptRunner(
                mockPythonScriptProvider.Object,
                mockPythonScriptGateway.Object,
                mockPythonVariableDefinitionProvider.Object,
                mockPythonResultParser.Object,
                mockLogger.Object);

            var pythonInputData = new PythonInputData
            {
                Variables = new List<Variable>
                {
                    new Variable(),
                    new Variable()
                }
            };

            var stoppingToken = new CancellationToken();

            await pythonScriptRunner.RunPythonProcessAsync(UserScript, pythonInputData, stoppingToken);

            mockPythonScriptGateway
                .Verify(x => x.RunPythonProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            // Two variables means two variable definitions.
            // Also there are two newlines between variables and user script, because each variable comes with its own newline
            var expectedPythonScript = $"Foo\r\n{VariableDefinition}\r\n{VariableDefinition}\r\n\r\n{UserScript}\r\nBar";
            Assert.Equal(expectedPythonScript, pythonScriptExecuted);
        }

        [Fact]
        public async Task ThatSubstitutesPythonScriptAndVariablesAndCallsResultParser()
        {
            const string ScriptTemplate = "Foo\r\n{{{VARIABLE_DEFINITIONS}}}\r\n{{{USER_CODE}}}\r\nBar";
            const string UserScript = "if 5 > 2:\r\n  print(\"Five is greater than two!\")";
            const string VariableDefinition = "fish = \"chips\"\r\nmushy_peas = 32.4";
            const string TestOutput = "FooBarWhizz";
            var variables = new List<Variable>
            {
                new Variable { Name = "Pop", Value = "Stop", VariableType = VariableType.String },
                new Variable { Name = "Pop", Value = 32, VariableType = VariableType.Integer },
            };

            var outputInCallBack = string.Empty;
            var variablesInCallBack = new List<Variable>();

            var mockPythonScriptProvider = new Mock<IPythonScriptProvider>();
            mockPythonScriptProvider.Setup(x => x.GetPythonScript()).Returns(ScriptTemplate);

            var mockPythonScriptGateway = new Mock<IPythonScriptGateway>();
            mockPythonScriptGateway
                .Setup(x => x.RunPythonProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(TestOutput));

            var mockPythonVariableDefinitionProvider = new Mock<IPythonVariableDefinitionProvider>();
            mockPythonVariableDefinitionProvider
                .Setup(x => x.GetPythonVariableDefinition(It.IsAny<Variable>()))
                .Returns(VariableDefinition);

            var mockPythonResultParser = new Mock<IPythonResultParser>();
            mockPythonResultParser
                .Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<List<Variable>>()))
                .Callback<string, List<Variable>>((x, y) => { outputInCallBack = x; variablesInCallBack = y; });

            var mockLogger = new Mock<ILogger<PythonScriptRunner>>();

            var pythonScriptRunner = new PythonScriptRunner(
                mockPythonScriptProvider.Object,
                mockPythonScriptGateway.Object,
                mockPythonVariableDefinitionProvider.Object,
                mockPythonResultParser.Object,
                mockLogger.Object);

            var pythonInputData = new PythonInputData
            {
                Variables = variables
            };

            var stoppingToken = new CancellationToken();

            await pythonScriptRunner.RunPythonProcessAsync(UserScript, pythonInputData, stoppingToken);

            mockPythonScriptGateway
                .Verify(x => x.RunPythonProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(TestOutput, outputInCallBack);
            Assert.Equal(variables, variablesInCallBack);
        }
    }
}