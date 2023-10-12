using DigitalMakerApi.Models;
using IronPython.Hosting;
using IronPython.Runtime;
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

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Lib");
            var libs = new[] {
              path
            };

            engine.SetSearchPaths(libs);

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

            var outputsInvoked = scope.GetVariable<PythonList>("outputs_invoked").ToList();
            var outputActions = new List<OutputAction>();
            foreach (var outputInvoked in outputsInvoked)
            {
                var pythonDict = outputInvoked as PythonDictionary;
                if (pythonDict == null) {
                    var msg = "Unexpected error: Output from python script that wasn't a PythonDictionary";
                    _logger.LogError(msg);
                    throw new InvalidOperationException(msg);
                }

                var dict = pythonDict.ToDictionary(x => Convert.ToString(x.Key) ?? string.Empty, x => Convert.ToString(x.Value) ?? string.Empty);

                if (!dict.TryGetValue("name", out var name) || !dict.TryGetValue("parameter", out var parameter))
                {
                    var msg = $"Unexpected error: Output from python script didn't have a name and/or parameter. Values: {dict.Select(x => x.Key + '/' + x.Value)}";
                    _logger.LogError(msg);
                    throw new InvalidOperationException(msg);
                }

                outputActions.Add(new OutputAction { ActionName = name.Trim('\"'), Argument = parameter.Trim('\"') });
            }

            return new PythonOutputData(outputActions);
        }
    }
}
