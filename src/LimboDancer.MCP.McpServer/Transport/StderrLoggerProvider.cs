using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Transport;

/// <summary>
/// Logger provider that writes to stderr for stdio mode.
/// </summary>
public class StderrLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLevel;

    public StderrLoggerProvider() : this(LogLevel.Error)
    {
    }

    public StderrLoggerProvider(LogLevel minLevel)
    {
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new StderrLogger(categoryName, _minLevel);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private class StderrLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minLevel;

        public StderrLogger(string categoryName, LogLevel minLevel)
        {
            _categoryName = categoryName;
            _minLevel = minLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            Console.Error.WriteLine($"[{timestamp}] [{logLevel}] {_categoryName}: {message}");

            if (exception != null)
            {
                Console.Error.WriteLine(exception.ToString());
            }
        }
    }
}