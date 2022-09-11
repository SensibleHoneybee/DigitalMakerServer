using DigitalMakerApi.Models;
using System.Collections;
using System.Linq;

namespace DigitalMakerWorkerApp.PythonInterface
{
    public interface IPythonVariableDefinitionProvider
    {
        string GetPythonVariableDefinition(Variable variable);
    }
    public class PythonVariableDefinitionProvider : IPythonVariableDefinitionProvider
    {
        private readonly ILogger<PythonVariableDefinitionProvider> _logger;

        public PythonVariableDefinitionProvider(ILogger<PythonVariableDefinitionProvider> logger)
        {
            this._logger = logger;
        }

        public string GetPythonVariableDefinition(Variable variable)
        {
            if (variable.VariableType == VariableType.String)
            {
                var str = BasicTypeToString(variable.Name, variable.Value, VariableType.String);
                return $"{variable.Name} = {str}";
            }
            else if (variable.VariableType == VariableType.Integer)
            {
                var str = BasicTypeToString(variable.Name, variable.Value, VariableType.Integer);
                return $"{variable.Name} = {str}";
            }
            else if (variable.VariableType == VariableType.Float)
            {
                var str = BasicTypeToString(variable.Name, variable.Value, VariableType.Float);
                return $"{variable.Name} = {str}";
            }
            else if (variable.VariableType == VariableType.List)
            {
                var valueAsList = variable.Value as IList<dynamic>;
                if (valueAsList == null)
                {
                    var msg = $"Error: Variable {variable.Name} was supposed to be a list, but in fact had a {variable.Value.GetType()} type";
                    _logger.LogError(msg);
                    throw new InvalidOperationException(msg);
                }

                var listDefinitions = valueAsList.Select(x => BasicTypeToString(variable.Name, x));

                return $"{variable.Name} = [{string.Join(", ", listDefinitions)}]";
            }
            ////else if (variable.VariableType == VariableType.Dictionary)
            ////{
            ////}
            ////else if (variable.VariableType == VariableType.Boolean)
            ////{

            ////}
            else
            {
                var msg = $"Unknown variable type {variable.VariableType} for variable {variable.Name}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
        }

        private string BasicTypeToString(string variableName, dynamic value, string? forcedType = null)
        {
            var valueAsString = value as string;
            var valueAsInteger = value as int?;
            var valueAsDecimal = value as decimal?;
            var valueAsFloat = value as float?;
            var valueAsDouble = value as double?;
            if (valueAsString != null && (forcedType == null || forcedType == VariableType.String))
            {
                var escapedValueAsString = valueAsString.Replace("\\", "\\\\").Replace("\r\n", "\\n").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\"", "\\\"").Replace("\'", "\\\'");
                return $"\"{escapedValueAsString}\"";
            }
            if (valueAsInteger.HasValue && (forcedType == null || forcedType == VariableType.Integer))
            {
                return $"{valueAsInteger}";
            }
            if (valueAsDecimal.HasValue && (forcedType == null || forcedType == VariableType.Float))
            {
                return $"{valueAsDecimal}";
            }
            else if (valueAsFloat.HasValue && (forcedType == null || forcedType == VariableType.Float))
            {
                return $"{valueAsFloat}";
            }
            else if (valueAsDouble.HasValue && (forcedType == null || forcedType == VariableType.Float))
            {
                return $"{valueAsDouble}";
            }
            else
            {
                var msg = $"Error: Variable {variableName} was supposed to be of type {forcedType}, but in fact had a {value.GetType()} type";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
        }
    }
}