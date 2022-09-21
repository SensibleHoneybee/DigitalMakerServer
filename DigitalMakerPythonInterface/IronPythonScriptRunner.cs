using System.Text;
using System.Text.RegularExpressions;
using DigitalMakerApi.Models;
using IronPython.Hosting;
using Microsoft.Extensions.Logging;

namespace DigitalMakerPythonInterface
{
    public class IronPythonScriptRunner : IPythonScriptRunner
    {
        private readonly IPythonScriptProvider _pythonScriptProvider;

        private readonly ILogger<IronPythonScriptRunner> _logger;

        public IronPythonScriptRunner(
            IPythonScriptProvider pythonScriptProvider,
            ILogger<IronPythonScriptRunner> logger)
        {
            this._pythonScriptProvider = pythonScriptProvider;
            this._logger = logger;
        }

        public async Task<PythonOutputData> RunPythonProcessAsync(string userSuppliedPythonCode, PythonInputData pythonInputData)
        {
            var defaultPythonScript = this._pythonScriptProvider.GetPythonScript();

            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();

            // Initialize the variables in python
            foreach (var variable in pythonInputData.Variables)
            {
                scope.SetVariable(variable.Name, variable.Value);
            }

            var actualPythonScript = defaultPythonScript
                .Replace("{{{USER_CODE}}}", userSuppliedPythonCode);

            try
            {
                await Task.Run(() => engine.Execute(actualPythonScript, scope));
            }
            catch (Exception ex)
            {
                var msg = $"An error occurred during python script opertation:\r\n{ex.Message}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            // Update our variables with their new values
            foreach (var variable in pythonInputData.Variables)
            {
                variable.Value = scope.GetVariable(variable.Name);
            }

            return new PythonOutputData(pythonInputData.Variables, new List<OutputAction>());
        }
    }
}
