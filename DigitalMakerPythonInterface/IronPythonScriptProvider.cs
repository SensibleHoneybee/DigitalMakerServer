using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DigitalMakerPythonInterface
{
    public class IronPythonScriptProvider : IPythonScriptProvider
    {
        private readonly ILogger<PythonScriptProvider> _logger;

        public IronPythonScriptProvider(ILogger<PythonScriptProvider> logger)
        {
            this._logger = logger;
        }

        public string GetPythonScript()
        {
            var assembly = typeof(PythonScriptRunner).GetTypeInfo().Assembly;
            var manifestResourceNames = assembly.GetManifestResourceNames().Where(x => x.Contains("DefaultScriptIron.py")).ToList();
            if (manifestResourceNames.Count != 1)
            {
                var msg = $"Expected one embedded resource file DefaultScriptIron.py, but instead found {manifestResourceNames.Count}.";
                this._logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            var pythonStream = assembly.GetManifestResourceStream(manifestResourceNames.Single());
            if (pythonStream == null)
            {
                var msg = $"Unexpectedly failed to load script file DefaultScriptIron.py ({manifestResourceNames.Single()}) into a stream.";
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