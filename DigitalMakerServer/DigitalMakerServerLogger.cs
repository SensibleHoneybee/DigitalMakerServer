using Microsoft.Extensions.Logging;
using System.Text;

namespace DigitalMakerServer
{
    public class DigitalMakerServerLogger<T> : ILogger<T>
    {
        private static readonly Dictionary<LogLevel, Amazon.Lambda.Core.LogLevel> LogLevelMap = new()
        {
            [LogLevel.Trace] = Amazon.Lambda.Core.LogLevel.Trace,
            [LogLevel.Debug] = Amazon.Lambda.Core.LogLevel.Debug,
            [LogLevel.Information] = Amazon.Lambda.Core.LogLevel.Information,
            [LogLevel.Warning] = Amazon.Lambda.Core.LogLevel.Warning,
            [LogLevel.Error] = Amazon.Lambda.Core.LogLevel.Error,
            [LogLevel.Critical] = Amazon.Lambda.Core.LogLevel.Critical
        };

        private readonly Amazon.Lambda.Core.ILambdaLogger lambdaLogger;

        public DigitalMakerServerLogger(Amazon.Lambda.Core.ILambdaLogger lambdaLogger)
        {
            this.lambdaLogger = lambdaLogger;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => LogLevelMap.ContainsKey(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var lambdaLogLevel = LogLevelMap[logLevel];

            var sb = new StringBuilder();

            sb.Append($"[{eventId.Id,2}: {logLevel,-12}] - ");
            sb.Append($"{formatter(state, exception)}");

            this.lambdaLogger.Log(lambdaLogLevel, sb.ToString());
        }
    }
}
