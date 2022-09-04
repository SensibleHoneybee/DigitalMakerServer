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
            var mockLogger1 = new Mock<ILogger<PythonScriptGateway>>();
            var mockLogger2 = new Mock<ILogger<PythonScriptRunner>>();
            var mockLogger3 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonScriptGateway = new PythonScriptGateway(mockLogger1.Object);
            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger3.Object);

            var pythonScriptRunner = new PythonScriptRunner(pythonScriptGateway, pythonVariableDefinitionProvider, mockLogger2.Object);

            var userSuppliedPythonCode = string.Empty;

            var pythonData = new PythonInputData
            {
                Variables = new List<Variable>
                {
                    new Variable
                    {
                        Name = "Tom",
                        VariableType = "Dick",
                        Value = "Harry"
                    }
                }
            };

            var result = await pythonScriptRunner.RunPythonProcessAsync(userSuppliedPythonCode, pythonData, new CancellationToken());


        }
    }
}
