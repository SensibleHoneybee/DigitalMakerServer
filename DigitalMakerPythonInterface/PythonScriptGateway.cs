using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DigitalMakerPythonInterface
{
    public interface IPythonScriptGateway
    {
        Task<string> RunPythonProcessAsync(string pythonCode, CancellationToken stoppingToken);
    }

    public class PythonScriptGateway : IPythonScriptGateway
    {
        private const int TimeoutInMilliseconds = 60000;

        private readonly ILogger<PythonScriptGateway> _logger;

        public PythonScriptGateway(ILogger<PythonScriptGateway> logger)
        {
            this._logger = logger;  
        }

        public async Task<string> RunPythonProcessAsync(string pythonCode, CancellationToken stoppingToken)
        {
            var tmpFile = Path.GetTempFileName();

            File.WriteAllText(tmpFile, pythonCode);

            try {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "python.exe";
                    process.StartInfo.Arguments = $"{tmpFile}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    var output = new StringBuilder();
                    var error = new StringBuilder();

                    using (var outputWaitHandle = new AutoResetEvent(false))
                    using (var errorWaitHandle = new AutoResetEvent(false))
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                error.AppendLine(e.Data);
                            }
                        };

                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        await process.WaitForExitAsync(stoppingToken);

                        if (await Task.Run(() => outputWaitHandle.WaitOne(TimeoutInMilliseconds)) &&
                            await Task.Run(() => errorWaitHandle.WaitOne(TimeoutInMilliseconds)))
                        {
                            if (process.ExitCode != 0)
                            {
                                var msg = $"The python script returned an error:\r\n{error.ToString()}";
                                _logger.LogError(msg);
                                throw new InvalidOperationException(msg);
                            }

                            return output.ToString();
                        }
                        else
                        {
                            var msg = "Process timed out";
                            _logger.LogError(msg);
                            throw new InvalidOperationException(msg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = $"An error occurred during python script opertation:\r\n{ex.Message}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }
    }
}
