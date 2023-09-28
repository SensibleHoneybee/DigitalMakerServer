using DigitalMakerApi.Models;
using DigitalMakerPythonInterface;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMakerWorkerApp.Tests
{
    public class PythonScriptIntegrationTests
    {
        [Fact]
        public async Task TestRunIronPythonScript()
        {
            var mockLogger0 = new Mock<ILogger<PythonScriptProvider>>();
            var mockLogger2 = new Mock<ILogger<IronPythonScriptRunner>>();

            var pythonScriptProvider = new IronPythonScriptProvider(mockLogger0.Object);

            var pythonScriptRunner = new IronPythonScriptRunner(
                pythonScriptProvider,
                mockLogger2.Object);

            var userSuppliedPythonCode =
                "name = \"Fred\"\r\n" +
                "output(\"message_customer\", \"Hello, \" + name)\r\n";

            var pythonData = new PythonInputData
            {
            };

            var result = await pythonScriptRunner.RunPythonProcessAsync(userSuppliedPythonCode, pythonData);
            
            Assert.Equal(1, result.OutputActions.Count);

            var outputAction1 = result.OutputActions[0];
            Assert.Equal("message_customer", outputAction1.ActionName);
            Assert.Equal("Hello, Fred", outputAction1.Argument);
        }
    }
}
