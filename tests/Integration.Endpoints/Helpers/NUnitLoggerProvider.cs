using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Integration.Endpoints.Helpers;

internal sealed class NUnitLoggerProvider(TextWriter output) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new NUnitLogger(output, categoryName);

    public void Dispose() { }

    private sealed class NUnitLogger(TextWriter output, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = $"[{logLevel}] {category}: {formatter(state, exception)}";
            if (exception is not null)
            {
                message += Environment.NewLine + exception;
            }

            output.WriteLine(message);
        }
    }
}
