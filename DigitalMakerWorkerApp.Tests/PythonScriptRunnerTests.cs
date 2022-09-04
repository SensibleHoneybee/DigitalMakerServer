using DigitalMakerWorkerApp.PythonInterface;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMakerWorkerApp.Tests
{

    public class PythonScriptRunnerTests
    {
        [Fact]
        public async Task ThatLoadsPythonScript()
        {
            var mockPythonScriptGateway = new Mock<IPythonScriptGateway>();
            var mockPythonVariableDefinitionProvider = new Mock<IPythonVariableDefinitionProvider>();
            var mockLogger = new Mock<ILogger<PythonScriptRunner>>();

            var pythonScriptRunner = new PythonScriptRunner(
                mockPythonScriptGateway.Object,
                mockPythonVariableDefinitionProvider.Object,
                mockLogger.Object);

            var stoppingToken = new CancellationToken();

            await pythonScriptRunner.RunPythonProcessAsync("my_function()", new PythonInputData(), stoppingToken);
        }
    }
}