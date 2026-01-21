using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Logging;

public sealed class AvaloniaLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private static long _logEntryIdCounter = 0;
    private readonly AvaloniaLogStore _store;
    private readonly ConcurrentDictionary<string, AvaloniaLogger> _loggers = new(StringComparer.Ordinal);

    private IExternalScopeProvider? _scopeProvider;

    public AvaloniaLoggerProvider(AvaloniaLogStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new AvaloniaLogger(name, _store, () => _scopeProvider));

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    public void Dispose() => _loggers.Clear();

    private sealed class AvaloniaLogger : ILogger
    {
        private readonly string _category;
        private readonly AvaloniaLogStore _store;
        private readonly Func<IExternalScopeProvider?> _getScopeProvider;

        public AvaloniaLogger(string category, AvaloniaLogStore store, Func<IExternalScopeProvider?> getScopeProvider)
        {
            _category = category;
            _store = store;
            _getScopeProvider = getScopeProvider;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            var scopeProvider = _getScopeProvider();
            return scopeProvider?.Push(state) ?? NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
                throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);

            var scopeProvider = _getScopeProvider();
            if (scopeProvider is not null)
            {
                scopeProvider.ForEachScope((scope, _) =>
                {
                    if (scope is null)
                        return;

                    if (message.Length == 0)
                        message = scope.ToString() ?? string.Empty;
                    else
                        message = $"{message} | {scope}";
                }, state: (object?)null);
            }

            var logId = Interlocked.Increment(ref _logEntryIdCounter);
            _store.Add(new AvaloniaLogEntry(
                Id: logId,
                TimestampUtc: DateTimeOffset.UtcNow,
                Level: logLevel,
                Category: _category,
                EventId: eventId,
                Message: message,
                Exception: exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

