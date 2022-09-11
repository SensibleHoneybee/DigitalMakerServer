using DigitalMakerApi.Models;
using DigitalMakerPythonInterface;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMakerWorkerAppTests.PythonInterface
{
    public class PythonVariableDefinitionProviderTests
    {
        [Fact]
        public void TestStringVariable()
        {
            var mockLogger1 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger1.Object);

            var variable = new Variable
            {
                Name = "fish",
                VariableType = VariableType.String,
                Value = "chips"
            };

            var theDefinition = pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);

            Assert.Equal("fish = \"chips\"", theDefinition);
        }

        [Fact]
        public void TestIntVariable()
        {
            var mockLogger1 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger1.Object);

            var variable = new Variable
            {
                Name = "fish",
                VariableType = VariableType.Integer,
                Value = 32
            };

            var theDefinition = pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);

            Assert.Equal("fish = 32", theDefinition);
        }

        [Fact]
        public void TestFloatVariableAsDecimal()
        {
            var mockLogger1 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger1.Object);

            var variable = new Variable
            {
                Name = "fish",
                VariableType = VariableType.Float,
                Value = 32.4m
            };

            var theDefinition = pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);

            Assert.Equal("fish = 32.4", theDefinition);
        }

        [Fact]
        public void ThatFloatVariableWithStringValueFails()
        {
            var mockLogger1 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger1.Object);

            var variable = new Variable
            {
                Name = "fish",
                VariableType = VariableType.Float,
                Value = "32.4m"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable));

            Assert.Equal("Error: Variable fish was supposed to be of type Float, but in fact had a System.String type", ex.Message);
        }

        [Fact]
        public void ThatIntVariableWithDoubleValueFails()
        {
            var mockLogger1 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger1.Object);

            var variable = new Variable
            {
                Name = "fish",
                VariableType = VariableType.Integer,
                Value = 32.4d
            };

            var ex = Assert.Throws<InvalidOperationException>(() => pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable));

            Assert.Equal("Error: Variable fish was supposed to be of type Integer, but in fact had a System.Double type", ex.Message);
        }

        [Fact]
        public void TestFloatVariableAsDouble()
        {
            var mockLogger1 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger1.Object);

            var variable = new Variable
            {
                Name = "fish",
                VariableType = VariableType.Float,
                Value = 32.4d
            };

            var theDefinition = pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);

            Assert.Equal("fish = 32.4", theDefinition);
        }

        [Fact]
        public void TestListVariable()
        {
            var mockLogger1 = new Mock<ILogger<PythonVariableDefinitionProvider>>();

            var pythonVariableDefinitionProvider = new PythonVariableDefinitionProvider(mockLogger1.Object);

            var variable = new Variable
            {
                Name = "fish",
                VariableType = VariableType.List,
                Value = new List<object> { 32.4d, "chips" }
            };

            var theDefinition = pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);

            Assert.Equal("fish = [32.4, \"chips\"]", theDefinition);
        }
    }
}
