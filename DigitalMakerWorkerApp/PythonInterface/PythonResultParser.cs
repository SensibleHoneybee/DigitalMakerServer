using DigitalMakerApi.Helpers;
using DigitalMakerApi.Models;
using Newtonsoft.Json;
using System.Collections;
using System.Linq;
using System.Text;

namespace DigitalMakerWorkerApp.PythonInterface
{
    public interface IPythonResultParser
    {
        PythonOutputData Parse(string pythonOutput, List<Variable> variables);
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
            var variableDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(pythonOutput);

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
    }
}