using DigitalMakerApi.Models;
using DigitalMakerWorkerApp.PythonInterface;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMakerWorkerApp.Tests
{
    public class PythonScriptIntegrationTests
    {
        [Fact]
        public async Task TestRunPythonScript()
        {
            var mockLogger0 = new Mock<ILogger<PythonScriptProvider>>();
            var mockLogger1 = new Mock<ILogger<PythonScriptGateway>>();
            var mockLogger2 = new Mock<ILogger<PythonScriptRunner>>();
            var mockLogger3 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonScriptProvider = new PythonScriptProvider(mockLogger0.Object);
            var pythonScriptGateway = new PythonScriptGateway(mockLogger1.Object);
            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger3.Object);

            var pythonScriptRunner = new PythonScriptRunner(
                pythonScriptProvider,
                pythonScriptGateway,
                pythonVariableDefinitionProvider,
                mockLogger2.Object);

            var userSuppliedPythonCode = string.Empty;

            var pythonData = new PythonInputData
            {
                Variables = new List<Variable>
                {
                    new Variable
                    {
                        Name = "fish_and_chips",
                        VariableType = VariableType.String,
                        Value = "Mushy\r\npeas, \"fo'ld\\bold\""
                    }
                }
            };

            var result = await pythonScriptRunner.RunPythonProcessAsync(userSuppliedPythonCode, pythonData, new CancellationToken());

            File.WriteAllText("C:\\temp\\MyFile.txt", result);
        }
    }
}
