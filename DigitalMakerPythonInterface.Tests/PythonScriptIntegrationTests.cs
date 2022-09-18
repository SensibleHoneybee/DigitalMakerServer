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
            var mockLogger3 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonScriptProvider = new IronPythonScriptProvider(mockLogger0.Object);
            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger3.Object);

            var pythonScriptRunner = new IronPythonScriptRunner(
                pythonScriptProvider,
                pythonVariableDefinitionProvider,
                mockLogger2.Object);

            var userSuppliedPythonCode =
                "name = \"Fred\"\r\n" +
                "output(\"message_customer\", \"Hello,\\n\" + name)\r\n" +
                "a_float = a_float + 1.2\r\n" +
                "a_float = a_float + 3\r\n" +
                "fish_and_chips = fish_and_chips + \"\\r\\nBiggins\"";


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

            var result = await pythonScriptRunner.RunPythonProcessAsync(userSuppliedPythonCode, pythonData);

            Assert.Equal(2, result.Variables.Count);

            var variable1 = result.Variables[0];
            Assert.Equal("fish_and_chips", variable1.Name);
            Assert.Equal(VariableType.String, variable1.VariableType);
            Assert.Equal("Mushy\r\npeas, \"fo'ld\\bold\"\r\nBiggins", variable1.Value);

            var variable2 = result.Variables[1];
            Assert.Equal("a_float", variable2.Name);
            Assert.Equal(VariableType.Float, variable2.VariableType);
            Assert.Equal(109.8, variable2.Value); // The user-supplied code above added 1.2 and 3 to the original 105.6
        }
    }
}
