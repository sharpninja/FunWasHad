using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace FWH.Mobile.Logging;

/// <summary>
/// Logger provider that writes all logs to Android ADB logcat via Console.WriteLine.
/// On Android, Console.WriteLine automatically goes to logcat.
/// </summary>
public sealed class AndroidLogcatLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, AndroidLogcatLogger> _loggers = new(StringComparer.Ordinal);

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new AndroidLogcatLogger(name));

    public void Dispose() => _loggers.Clear();

    private sealed class AndroidLogcatLogger : ILogger
    {
        private readonly string _category;

        public AndroidLogcatLogger(string category)
        {
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter == null)
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            // Suppress third-party icon-provider log noise (we use Font Awesome only).
            if (message.Contains("MaterialIcon", StringComparison.OrdinalIgnoreCase)
                && !_category.StartsWith("FWH.", StringComparison.OrdinalIgnoreCase))
                return;

            // Format log message for logcat
            var logLevelPrefix = logLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "FATAL",
                _ => "LOG"
            };

            // Create a readable category abbreviation
            // For long categories, show the class name (last part after '.') or last meaningful segment
            var categoryShort = GetReadableCategoryName(_category);
            var logMessage = $"[{logLevelPrefix}] {categoryShort}: {message}";

            if (exception != null)
            {
                logMessage += $"\nException: {exception.GetType().Name}: {exception.Message}";
                if (exception.StackTrace != null)
                {
                    logMessage += $"\n{exception.StackTrace}";
                }
            }

            // Write to console - on Android this goes to logcat
            Console.WriteLine(logMessage);
            System.Diagnostics.Debug.WriteLine(logMessage);
        }

        private static string GetReadableCategoryName(string category)
        {
            if (string.IsNullOrEmpty(category))
                return category;

            // If category is short enough, use it as-is
            if (category.Length <= 30)
                return category;

            // Extract the class name (last segment after '.')
            var lastDotIndex = category.LastIndexOf('.');
            if (lastDotIndex >= 0 && lastDotIndex < category.Length - 1)
            {
                var className = category.Substring(lastDotIndex + 1);
                // If class name alone is reasonable, use it
                if (className.Length <= 30)
                    return className;

                // Otherwise, try to include one namespace level
                var secondLastDotIndex = category.LastIndexOf('.', lastDotIndex - 1);
                if (secondLastDotIndex >= 0)
                {
                    var namespaceAndClass = category.Substring(secondLastDotIndex + 1);
                    return namespaceAndClass.Length <= 30 ? namespaceAndClass : className;
                }
            }

            // Fallback: use last 30 characters if we can't parse it
            return category.Length > 30 ? category.Substring(category.Length - 30) : category;
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
