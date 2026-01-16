using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Reflection;

namespace FWH.Common.Chat.Tests.TestFixtures;

public class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentBag<LogEntry> _entries = new();
    private readonly AsyncLocal<Stack<object>> _scopeStack = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();

    public event Action<LogEntry>? LogAdded;

    public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _entries, _scopeStack, OnLogAdded);

    public void Dispose() { }

    private void OnLogAdded(LogEntry e)
    {
        _entries.Add(e);
        LogAdded?.Invoke(e);
    }

    public record LogEntry(string Category, LogLevel Level, EventId EventId, string Message, Exception? Exception, IEnumerable<KeyValuePair<string, object?>> State, IEnumerable<object> Scopes, IEnumerable<IDictionary<string, object?>> ScopesParsed);

    private class TestLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentBag<LogEntry> _entries;
        private readonly AsyncLocal<Stack<object>> _scopeStack;
        private readonly Action<LogEntry> _onAdded;

        public TestLogger(string category, ConcurrentBag<LogEntry> entries, AsyncLocal<Stack<object>> scopeStack, Action<LogEntry> onAdded)
        {
            _category = category;
            _entries = entries;
            _scopeStack = scopeStack;
            _onAdded = onAdded;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            var stack = _scopeStack.Value;
            if (stack == null)
            {
                stack = new Stack<object>();
                _scopeStack.Value = stack;
            }
            stack.Push(state!);
            return new DisposableScope(_scopeStack);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var statePairs = new List<KeyValuePair<string, object?>>();
            if (state is IEnumerable<KeyValuePair<string, object?>> kv)
            {
                statePairs.AddRange(kv);
            }

            var scopes = new List<object>();
            var scopesParsed = new List<IDictionary<string, object?>>();
            var stack = _scopeStack.Value;
            if (stack != null)
            {
                foreach (var s in stack.Reverse())
                {
                    scopes.Add(s);
                    // parse scope into dictionary
                    var dict = ParseScopeObject(s);
                    if (dict != null)
                        scopesParsed.Add(dict);
                }
            }

            var entry = new LogEntry(_category, logLevel, eventId, message, exception, statePairs, scopes, scopesParsed);
            _onAdded(entry);
        }

        private static IDictionary<string, object?>? ParseScopeObject(object? scope)
        {
            if (scope == null) return null;
            // If it's an enumerable of key/value pairs
            if (scope is IEnumerable<KeyValuePair<string, object?>> kvpEnum)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var kv in kvpEnum) dict[kv.Key] = kv.Value;
                return dict;
            }

            // If it's an IDictionary<string, object?>
            if (scope is IDictionary<string, object?> dictObj)
            {
                return new Dictionary<string, object?>(dictObj);
            }

            // If it's an anonymous/object with properties, reflect
            var type = scope.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (props.Length > 0)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var p in props)
                {
                    try
                    {
                        dict[p.Name] = p.GetValue(scope);
                    }
                    catch { dict[p.Name] = null; }
                }
                return dict;
            }

            // fallback to ToString
            return new Dictionary<string, object?> { ["_"] = scope.ToString() };
        }

        private class DisposableScope : IDisposable
        {
            private readonly AsyncLocal<Stack<object>> _scopeStack;
            public DisposableScope(AsyncLocal<Stack<object>> scopeStack)
            {
                _scopeStack = scopeStack;
            }
            public void Dispose()
            {
                var stack = _scopeStack.Value;
                if (stack != null && stack.Count > 0)
                {
                    stack.Pop();
                }
            }
        }
    }

    public async Task<LogEntry?> WaitForEntryAsync(Func<LogEntry, bool> predicate, int timeoutMs = 2000)
    {
        var tcs = new TaskCompletionSource<LogEntry?>();
        void Handler(LogEntry e)
        {
            try
            {
                if (predicate(e)) tcs.TrySetResult(e);
            }
            catch { }
        }

        LogAdded += Handler;

        // Check existing entries first
        foreach (var e in _entries)
        {
            if (predicate(e))
            {
                LogAdded -= Handler;
                return e;
            }
        }

        var task = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
        LogAdded -= Handler;
        if (task == tcs.Task) return await tcs.Task;
        return null;
    }
}
