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
            var mockLogger4 = new Mock<ILogger<PythonResultParser>>();

            var pythonScriptProvider = new PythonScriptProvider(mockLogger0.Object);
            var pythonScriptGateway = new PythonScriptGateway(mockLogger1.Object);
            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger3.Object);
            var pythonResultParser = new PythonResultParser(mockLogger4.Object);

            var pythonScriptRunner = new PythonScriptRunner(
                pythonScriptProvider,
                pythonScriptGateway,
                pythonVariableDefinitionProvider,
                pythonResultParser,
                mockLogger2.Object);

            var userSuppliedPythonCode =
                "name = \"Fred\"\r\n" +
                "output(\"message_customer\", \"Hello,\\n\" + name)";

            var pythonData = new PythonInputData
            {
                Variables = new List<Variable>
                {
                    new Variable
                    {
                        Name = "fish_and_chips",
                        VariableType = VariableType.String,
                        Value = "Mushy\r\npeas, \"fo'ld\\bold\""
                    },
                    new Variable
                    {
                        Name = "a_float",
                        VariableType = VariableType.Float,
                        Value = 105.6m
                    }
                }
            };

            var result = await pythonScriptRunner.RunPythonProcessAsync(userSuppliedPythonCode, pythonData, new CancellationToken());

            File.WriteAllText("C:\\temp\\MyFile.txt", result);

             
        }
    }
}
