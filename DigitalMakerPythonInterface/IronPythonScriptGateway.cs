using System.Diagnostics;
using System.Text;
using IronPython.Hosting;
using Microsoft.Extensions.Logging;

namespace DigitalMakerPythonInterface
{
    public class IronPythonScriptGateway : IPythonScriptGateway
    {
        private const int TimeoutInMilliseconds = 60000;

        private readonly ILogger<PythonScriptGateway> _logger;

        public IronPythonScriptGateway(ILogger<PythonScriptGateway> logger)
        {
            this._logger = logger;  
        }

        public async Task<string> RunPythonProcessAsync(string pythonCode, CancellationToken stoppingToken)
        {
            try {
                var engine = Python.CreateEngine();
                var scope = engine.CreateScope();
                engine.Execute(pythonCode, scope);
                dynamic runPythonScript = scope.GetVariable("run_python_script");
                var g = runPythonScript();
            }
            catch (Exception ex)
            {
                var msg = $"An error occurred during python script opertation:\r\n{ex.Message}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            return "";
        }
    }
}
