﻿using System.Reflection;
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
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts/DefaultScriptIron.py");

            if (!File.Exists(path))
            {
                var msg = $"Expected a file of name {path} but did not find one.";
                this._logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            using (var streamReader = new StreamReader(path))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}