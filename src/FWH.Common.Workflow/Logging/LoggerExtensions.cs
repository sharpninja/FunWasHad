using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Logging;

/// <summary>
/// Extension methods for structured logging with correlation IDs.
/// </summary>
public static partial class LoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "Operation started: {Operation}")]
    private static partial void LogOperationStartMessage(ILogger logger, string operation);

    [LoggerMessage(LogLevel.Information, "Operation completed: {Operation} in {DurationMs}ms")]
    private static partial void LogOperationCompleteMessage(ILogger logger, string operation, double durationMs);

    [LoggerMessage(LogLevel.Error, "Operation failed: {Operation}")]
    private static partial void LogOperationFailureMessage(ILogger logger, Exception exception, string operation);

    /// <summary>
    /// Begins a logging scope with correlation ID and additional properties.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="correlationIdService">Service to get correlation ID</param>
    /// <param name="operation">Name of the operation being performed</param>
    /// <param name="additionalProperties">Additional properties to include in scope</param>
    /// <returns>Disposable scope</returns>
    public static IDisposable BeginCorrelatedScope(
        this ILogger logger,
        ICorrelationIdService correlationIdService,
        string operation,
        IDictionary<string, object>? additionalProperties = null)
    {
        ArgumentNullException.ThrowIfNull(correlationIdService);
        ArgumentNullException.ThrowIfNull(logger);
        var properties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationIdService.GetCorrelationId(),
            ["Operation"] = operation,
            ["Timestamp"] = DateTime.UtcNow
        };

        if (additionalProperties != null)
        {
            foreach (var kvp in additionalProperties)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }

        return logger.BeginScope(properties) ?? new NullDisposable();
    }

    /// <summary>
    /// Logs an operation start with correlation ID.
    /// </summary>
    public static void LogOperationStart(
        this ILogger logger,
        ICorrelationIdService correlationIdService,
        string operation,
        IDictionary<string, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(correlationIdService);
        ArgumentNullException.ThrowIfNull(logger);
        var allProperties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationIdService.GetCorrelationId(),
            ["Operation"] = operation,
            ["Phase"] = "Start"
        };

        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                allProperties[kvp.Key] = kvp.Value;
            }
        }

        using (logger.BeginScope(allProperties))
        {
            LogOperationStartMessage(logger, operation);
        }
    }

    /// <summary>
    /// Logs an operation completion with correlation ID and duration.
    /// </summary>
    public static void LogOperationComplete(
        this ILogger logger,
        ICorrelationIdService correlationIdService,
        string operation,
        TimeSpan duration,
        IDictionary<string, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(correlationIdService);
        ArgumentNullException.ThrowIfNull(logger);
        var allProperties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationIdService.GetCorrelationId(),
            ["Operation"] = operation,
            ["Phase"] = "Complete",
            ["DurationMs"] = duration.TotalMilliseconds
        };

        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                allProperties[kvp.Key] = kvp.Value;
            }
        }

        using (logger.BeginScope(allProperties))
        {
            LogOperationCompleteMessage(logger, operation, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Logs an operation failure with correlation ID, duration, and exception.
    /// </summary>
    public static void LogOperationFailure(
        this ILogger logger,
        ICorrelationIdService correlationIdService,
        string operation,
        Exception exception,
        TimeSpan? duration = null,
        IDictionary<string, object>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(correlationIdService);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(logger);
        var allProperties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationIdService.GetCorrelationId(),
            ["Operation"] = operation,
            ["Phase"] = "Failed",
            ["ErrorType"] = exception.GetType().Name
        };

        if (duration.HasValue)
        {
            allProperties["DurationMs"] = duration.Value.TotalMilliseconds;
        }

        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                allProperties[kvp.Key] = kvp.Value;
            }
        }

        using (logger.BeginScope(allProperties))
        {
            LogOperationFailureMessage(logger, exception, operation);
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
