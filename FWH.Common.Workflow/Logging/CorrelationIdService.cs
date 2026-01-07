using System;

namespace FWH.Common.Workflow.Logging;

/// <summary>
/// Service for managing correlation IDs across the application.
/// Correlation IDs help trace requests/operations through logs.
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>
    /// Gets the current correlation ID for this execution context.
    /// </summary>
    string GetCorrelationId();

    /// <summary>
    /// Sets a new correlation ID for this execution context.
    /// </summary>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new correlation ID.
    /// </summary>
    string GenerateCorrelationId();
}

/// <summary>
/// Thread-safe implementation of correlation ID service using AsyncLocal.
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public string GetCorrelationId()
    {
        return _correlationId.Value ?? GenerateCorrelationId();
    }

    public void SetCorrelationId(string correlationId)
    {
        ArgumentNullException.ThrowIfNull(correlationId);
        _correlationId.Value = correlationId;
    }

    public string GenerateCorrelationId()
    {
        var newId = Guid.NewGuid().ToString("N");
        _correlationId.Value = newId;
        return newId;
    }
}
