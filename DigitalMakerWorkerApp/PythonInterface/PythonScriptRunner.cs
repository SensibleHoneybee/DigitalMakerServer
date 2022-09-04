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
        private readonly IPythonScriptGateway _pythonScriptGateway;

        private readonly IPythonVariableDefinitionProvider _pythonVariableDefinitionProvider;

        private readonly ILogger<PythonScriptRunner> _logger;

        public PythonScriptRunner(IPythonScriptGateway pythonScriptGateway,
            IPythonVariableDefinitionProvider pythonVariableDefinitionProvider,
            ILogger<PythonScriptRunner> logger)
        {
            this._pythonScriptGateway = pythonScriptGateway;
            this._pythonVariableDefinitionProvider = pythonVariableDefinitionProvider;
            this._logger = logger;
        }

        public Task<string> RunPythonProcessAsync(string userSuppliedPythonCode, PythonInputData pythonInputData, CancellationToken stoppingToken)
        {
            var defaultPythonScript = this.ReadDefaultScript();

            // Construct python initializers for the variables
            var variableDefinitions = new StringBuilder();
            foreach (var variable in pythonInputData.Variables)
            {
                var definition = this._pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);
                variableDefinitions.AppendLine(definition);
            }

            var actualPythonScript = string.Format(defaultPythonScript, userSuppliedPythonCode);

            return this._pythonScriptGateway.RunPythonProcessAsync(actualPythonScript, stoppingToken);
        }

        private string ReadDefaultScript()
        {
            var assembly = typeof(PythonScriptRunner).GetTypeInfo().Assembly;
            var manifestResourceNames = assembly.GetManifestResourceNames().Where(x => x.Contains("DefaultScript.py")).ToList();
            if (manifestResourceNames.Count != 1)
            {
                var msg = $"Expected one embedded resource file DefaultScript.py, but instead found {manifestResourceNames.Count}.";
                this._logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            var pythonStream = assembly.GetManifestResourceStream(manifestResourceNames.Single());
            if (pythonStream == null)
            {
                var msg = $"Unexpectedly failed to load script file DefaultScript.py ({manifestResourceNames.Single()}) into a stream.";
                this._logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            using (var streamReader = new StreamReader(pythonStream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
