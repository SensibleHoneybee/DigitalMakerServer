using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DigitalMakerPythonInterface
{
    public class PythonService
    {
        private readonly ILogger<PythonService> _logger;

        public PythonService(ILogger<PythonService> logger)
        {
            this._logger = logger;
        }

        public async Task OpenConnectionAsync(CancellationToken token)
        {

        }


    }
}