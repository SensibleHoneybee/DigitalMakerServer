using DigitalMakerApi.Models;
using DigitalMakerPythonInterface;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMakerWorkerAppTests.PythonInterface
{
    public class PythonResultParserTests
    {
        #region Parse tests
        [Fact]
        public void ThatParseAltersStringVariableAsRequired()
        {
            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            const string PythonData = "{\"__name__\":\"__main__\",\"fish_and_chips\":\"Mushy peas\"}";

            var variables = new List<Variable>
            {
                new Variable { Name = "fish_and_chips", Value = "Haddock and mash", VariableType = VariableType.String }
            };

            var result = pythonResultParser.Parse(PythonData, variables);

            Assert.Single(result.Variables);
            Assert.Equal("Mushy peas", result.Variables[0].Value);
        }

        [Fact]
        public void ThatParseAltersFloatVariableAsRequired()
        {
            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            const string PythonData = "{\"__name__\":\"__main__\",\"fish_and_chips\":\"32.9\"}";

            var variables = new List<Variable>
            {
                new Variable { Name = "fish_and_chips", Value = 104.6, VariableType = VariableType.Float }
            };

            var result = pythonResultParser.Parse(PythonData, variables);

            Assert.Single(result.Variables);
            Assert.Equal(32.9m, result.Variables[0].Value);
        }

        [Fact]
        public void ThatParseFailsIfVariableIsNotPresent()
        {
            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            const string PythonData = "{'__name__': '__main__', 'fish_and_chips': 'Mushy peas'}";

            var variables = new List<Variable>
            {
                new Variable { Name = "Ping", Value = "Pong", VariableType = VariableType.String },
                new Variable { Name = "Ding", Value = "Dong", VariableType = VariableType.String }
            };

            var ex = Assert.Throws<InvalidOperationException>(() => pythonResultParser.Parse(PythonData, variables));

            Assert.Equal("Could not find variable Ping in python output " + PythonData, ex.Message);
        }


        #endregion
    }
}