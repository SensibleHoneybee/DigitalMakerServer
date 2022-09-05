using DigitalMakerApi.Helpers;
using DigitalMakerApi.Models;
using System.Collections;
using System.Linq;
using System.Text;

namespace DigitalMakerWorkerApp.PythonInterface
{
    public interface IPythonResultParser
    {
        PythonOutputData Parse(string pythonOutput, List<Variable> variables);

        List<KeyValuePair<string, string>> ParseIntoVariableList(string pythonOutput);
    }
    public class PythonResultParser : IPythonResultParser
    {
        private readonly ILogger<PythonResultParser> _logger;

        public PythonResultParser(ILogger<PythonResultParser> logger)
        {
            this._logger = logger;
        }

        public PythonOutputData Parse(string pythonOutput, List<Variable> variables)
        {
            var variableList = ParseIntoVariableList(pythonOutput);
            var variableDict = new Dictionary<string, string>(variableList);

            // Now extract values of variables
            foreach (var variable in variables)
            {
                string variableValue;
                if (!variableDict.TryGetValue(variable.Name, out variableValue))
                {
                    var msg = $"Could not find variable {variable.Name} in python output {pythonOutput}";
                    _logger.LogError(msg);
                    throw new InvalidOperationException(msg);
                }

                variable.SetValueFromString(variableValue);
            }

            return new PythonOutputData(variables, new List<OutputAction>());
        }

        public List<KeyValuePair<string, string>> ParseIntoVariableList(string pythonOutput)
        {
            if (pythonOutput == "{}")
            {
                // Empty list case.
                return new List<KeyValuePair<string, string>>();
            }

            // Output is of the form {'__name__': '__main__', 'fish_and_chips': 'Mushy peas'}
            // We therefore need to:
            // 1. Strip off leading and trailing {}
            // 2. Parse each unit separately into blocks of Name and Value. Noting that commas may occur inside strings.
            var charArray = pythonOutput.ToCharArray();
            if (charArray.Length < 2 || charArray[0] != '{' || charArray[charArray.Length - 1] != '}')
            {
                const string CurlyBraces = "{}";
                var msg = $"Expected start and end chars of python output to be {CurlyBraces} but instead it was {charArray[0]}{charArray[charArray.Length - 1]}. Python output: {pythonOutput}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            // Iterate through array, discounting first and last chars
            var parsingPoint = ParsingPoint.ExpectingNewRow;
            var results = new List<KeyValuePair<string, string>>();
            var currentKey = new StringBuilder();
            var currentValue = new StringBuilder();
            var syntaxError = false;
            var currentPos = 1;
            while (currentPos < charArray.Length - 1)
            {
                var currentChar = charArray[currentPos];

                switch (parsingPoint)
                {
                    case ParsingPoint.ExpectingNewRow:
                        // Row should start with the opening ' of the key
                        if (currentChar != '\'') { syntaxError = true; break; }
                        parsingPoint = ParsingPoint.InKey;
                        break;
                    case ParsingPoint.InKey:
                        if (currentChar == '\'')
                        {
                            // Terminating ' character of the key
                            parsingPoint = ParsingPoint.ExpectingInBetweenKeyAndValue1;
                        }
                        else
                        {
                            // Any other char is part of the key
                            currentKey.Append(currentChar);
                        }
                        break;
                    case ParsingPoint.ExpectingInBetweenKeyAndValue1:
                        // This char should be a colon
                        if (currentChar != ':') { syntaxError = true; break; }
                        parsingPoint = ParsingPoint.ExpectingInBetweenKeyAndValue2;
                        break;
                    case ParsingPoint.ExpectingInBetweenKeyAndValue2:
                        // This char should be a space
                        if (currentChar != ' ') { syntaxError = true; break; }
                        parsingPoint = ParsingPoint.ExpectingInBetweenKeyAndValue3;
                        break;
                    case ParsingPoint.ExpectingInBetweenKeyAndValue3:
                        // This char should be a ' to start the value
                        if (currentChar != '\'') { syntaxError = true; break; }
                        parsingPoint = ParsingPoint.InValue;
                        break;
                    case ParsingPoint.InValue:
                        if (currentChar == '\'')
                        {
                            // Terminating ' character of the value
                            parsingPoint = ParsingPoint.ExpectingInBetweenRows1;
                        }
                        else
                        {
                            // Any other char is part of the value
                            currentValue.Append(currentChar);
                        }
                        break;
                    case ParsingPoint.ExpectingInBetweenRows1:
                        // First end of row character is a comma
                        if (currentChar != ',') { syntaxError = true; break; }
                        parsingPoint = ParsingPoint.ExpectingInBetweenRows2;
                        break;
                    case ParsingPoint.ExpectingInBetweenRows2:
                        // Second end of row character is a space
                        if (currentChar != ' ') { syntaxError = true; break; }

                        // Row complete, add it to the results
                        results.Add(new KeyValuePair<string, string>(currentKey.ToString(), currentValue.ToString()));
                        currentKey.Clear();
                        currentValue.Clear();
                        parsingPoint = ParsingPoint.ExpectingNewRow;
                        break;
                }

                if (syntaxError)
                {
                    break;
                }

                currentPos++;
            }

            // When we get here, either there should have been a syntax error, or we should have broken
            // after the last row, in which case we're in between rows. Fail if there's an error.
            if (syntaxError || parsingPoint != ParsingPoint.ExpectingInBetweenRows1)
            {
                var msg = $"Syntax error in python output {pythonOutput} at position {currentPos}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            // Now add the last line (there must have been at least one line, as we handled the empty
            // case already at the top of this function)
            results.Add(new KeyValuePair<string, string>(currentKey.ToString(), currentValue.ToString()));

            return results;
        }

        private enum ParsingPoint
        {
            ExpectingNewRow = 1,
            InKey,
            ExpectingInBetweenKeyAndValue1,
            ExpectingInBetweenKeyAndValue2,
            ExpectingInBetweenKeyAndValue3,
            InValue,
            ExpectingInBetweenRows1,
            ExpectingInBetweenRows2
        }
    }
}