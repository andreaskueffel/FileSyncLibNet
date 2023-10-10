using Microsoft.Extensions.Logging;
using System;

namespace FileSyncLibNet.Logger
{
    internal class StringLogger : ILogger
    {
        public LogLevel MinimumLogLevel { get; set; }

        public StringLogger(Action<string> logAction)
        {
            LogAction = logAction;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public Action<string> LogAction { get; }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= MinimumLogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            LogAction.Invoke($"[{logLevel,-12}] {formatter(state, exception)}");
        }
    }
}
