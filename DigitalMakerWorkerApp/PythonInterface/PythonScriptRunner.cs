using System.Reflection;
using System.Text;

namespace DigitalMakerWorkerApp.PythonInterface
{
    public interface IPythonScriptRunner
    {
        Task<string> RunPythonProcessAsync(string userSuppliedPythonCode, PythonInputData pythonData, CancellationToken stoppingToken);
    }

    public class PythonScriptRunner : IPythonScriptRunner
    {
        private readonly IPythonScriptProvider _pythonScriptProvider;

        private readonly IPythonScriptGateway _pythonScriptGateway;

        private readonly IPythonVariableDefinitionProvider _pythonVariableDefinitionProvider;

        private readonly ILogger<PythonScriptRunner> _logger;

        public PythonScriptRunner(
            IPythonScriptProvider pythonScriptProvider,
            IPythonScriptGateway pythonScriptGateway,
            IPythonVariableDefinitionProvider pythonVariableDefinitionProvider,
            ILogger<PythonScriptRunner> logger)
        {
            this._pythonScriptProvider = pythonScriptProvider;
            this._pythonScriptGateway = pythonScriptGateway;
            this._pythonVariableDefinitionProvider = pythonVariableDefinitionProvider;
            this._logger = logger;
        }

        public Task<string> RunPythonProcessAsync(string userSuppliedPythonCode, PythonInputData pythonInputData, CancellationToken stoppingToken)
        {
            var defaultPythonScript = this._pythonScriptProvider.GetPythonScript();

            // Construct python initializers for the variables
            var variableDefinitions = new StringBuilder();
            foreach (var variable in pythonInputData.Variables)
            {
                var definition = this._pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);
                variableDefinitions.AppendLine(definition);
            }

            var actualPythonScript = string.Format(defaultPythonScript, variableDefinitions.ToString(), userSuppliedPythonCode);

            return this._pythonScriptGateway.RunPythonProcessAsync(actualPythonScript, stoppingToken);
        }
    }
}
