using DigitalMakerApi.Models;
using DigitalMakerWorkerApp.PythonInterface;
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

            const string PythonData = "{'__name__': '__main__', 'fish_and_chips': 'Mushy peas'}";

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

            const string PythonData = "{'__name__': '__main__', 'fish_and_chips': 32.9}";

            var variables = new List<Variable>
            {
                new Variable { Name = "fish_and_chips", Value = 104.6, VariableType = VariableType.Float }
            };

            var result = pythonResultParser.Parse(PythonData, variables);

            Assert.Single(result.Variables);
            Assert.Equal(32.9, result.Variables[0].Value);
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

        #region ParseIntoVariableList tests
        [Fact]
        public void ThatIntoVariableListFailsIfResultDoesNotStartWithBrace()
        {
            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            const string PythonData = "'__name__': '__main__', 'fish_and_chips': 'Mushy peas'}";

            var ex = Assert.Throws<InvalidOperationException>(() => pythonResultParser.ParseIntoVariableList(PythonData));

            Assert.Equal("Expected start and end chars of python output to be {} but instead it was '}. Python output: " + PythonData, ex.Message);
        }

        [Fact]
        public void ThatIntoVariableListFailsIfResultDoesNotEndWithBrace()
        {
            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            const string PythonData = "{'__name__': '__main__', 'fish_and_chips': 'Mushy peas'";

            var ex = Assert.Throws<InvalidOperationException>(() => pythonResultParser.ParseIntoVariableList(PythonData));

            Assert.Equal("Expected start and end chars of python output to be {} but instead it was {'. Python output: " + PythonData, ex.Message);
        }

        [Fact]
        public void ThatIntoVariableListFailsWithSyntaxError1()
        {
            // Missing opening '
            const string PythonData = "{__name__': '__main__', 'fish_and_chips': 'Mushy peas'}";
            const int ExpectedSyntaxErrorPos = 1;
            ThatIntoVariableListFailsWithSyntaxError(PythonData, ExpectedSyntaxErrorPos);
        }

        [Fact]
        public void ThatIntoVariableListFailsWithSyntaxError2()
        {
            // Slash instead of : after fish_and_chips
            const string PythonData = "{'__name__': '__main__', 'fish_and_chips'/ 'Mushy peas'}";
            const int ExpectedSyntaxErrorPos = 41;
            ThatIntoVariableListFailsWithSyntaxError(PythonData, ExpectedSyntaxErrorPos);
        }

        [Fact]
        public void ThatIntoVariableListFailsWithSyntaxError3()
        {
            // Missing space before fish_and_chips
            const string PythonData = "{'__name__': '__main__','fish_and_chips': 'Mushy peas'}";
            const int ExpectedSyntaxErrorPos = 24;
            ThatIntoVariableListFailsWithSyntaxError(PythonData, ExpectedSyntaxErrorPos);
        }

        [Fact]
        public void ThatIntoVariableListReadsIfProperlyFormatted()
        {
            // Missing space before fish_and_chips
            const string PythonData = "{'__name__': '__main__', 'fish_and_chips': 'Mushy peas'}";

            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            var result = pythonResultParser.ParseIntoVariableList(PythonData);

            Assert.Equal(2, result.Count);
            Assert.Equal("__name__", result[0].Key);
            Assert.Equal("__main__", result[0].Value);
            Assert.Equal("fish_and_chips", result[1].Key);
            Assert.Equal("Mushy peas", result[1].Value);
        }

        [Fact]
        public void ThatIntoVariableListReadsLongOutputIfProperlyFormatted()
        {
            // Missing space before fish_and_chips
            const string PythonData = "{'__name__': '__main__', '__doc__': None, '__package__': None, '__loader__': <_frozen_importlib_external.SourceFileLoader object at 0x00000125055349A0>, '__spec__': None, '__annotations__': {}, '__builtins__': <module 'builtins' (built-in)>, '__file__': 'C:\\Users\\steve\\AppData\\Local\\Temp\\tmpFC8B.tmp', '__cached__': None, 'outputs_invoked': [], 'my_function': <function my_function at 0x0000012505473E20>, 'sys': <module 'sys' (built-in)>, 'json': <module 'json' from 'C:\\Users\\steve\\AppData\\Local\\Programs\\Python\\Python310\\lib\\json\\__init__.py'>, 'fish_and_chips': 'Mushy peas, fold'}";

            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            var result = pythonResultParser.ParseIntoVariableList(PythonData);

            Assert.Equal(2, result.Count);
            Assert.Equal("__name__", result[0].Key);
            Assert.Equal("__main__", result[0].Value);
            Assert.Equal("fish_and_chips", result[1].Key);
            Assert.Equal("Mushy peas, fold", result[1].Value);
        }

        private void ThatIntoVariableListFailsWithSyntaxError(string pythonData, int expectedSyntaxErrorPos)
        {
            var mockLogger1 = new Mock<ILogger<PythonResultParser>>();

            var pythonResultParser = new PythonResultParser(mockLogger1.Object);

            var ex = Assert.Throws<InvalidOperationException>(() => pythonResultParser.ParseIntoVariableList(pythonData));

            Assert.Equal("Syntax error in python output " + pythonData + $" at position {expectedSyntaxErrorPos}", ex.Message);
        }
        #endregion
    }
}