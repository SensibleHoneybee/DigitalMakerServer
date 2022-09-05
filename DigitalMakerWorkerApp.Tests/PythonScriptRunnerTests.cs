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

            var mockLogger = new Mock<ILogger<PythonScriptRunner>>();

            var pythonScriptRunner = new PythonScriptRunner(
                mockPythonScriptProvider.Object,
                mockPythonScriptGateway.Object,
                mockPythonVariableDefinitionProvider.Object,
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
    }
}