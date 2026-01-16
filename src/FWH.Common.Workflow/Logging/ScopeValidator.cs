using System;
using System.Collections.Generic;
using System.Linq;

namespace FWH.Common.Workflow.Logging;

/// <summary>
/// Small runtime helpers to validate structured logging scopes.
/// Intended for tests and diagnostic checks where the logging provider exposes
/// parsed scope dictionaries (e.g. TestLoggerProvider).
/// </summary>
public static class ScopeValidator
{
    /// <summary>
    /// Return true when the provided scope dictionary contains all requested keys
    /// and none of the corresponding values are null.
    /// </summary>
    public static bool ContainsKeys(IDictionary<string, object?> scope, params string[] keys)
    {
        if (scope is null) throw new ArgumentNullException(nameof(scope));
        if (keys is null || keys.Length == 0) return true;
        return keys.All(k => scope.ContainsKey(k) && scope[k] != null);
    }

    /// <summary>
    /// Return true when any of the provided scope dictionaries contains all requested keys.
    /// </summary>
    public static bool AnyScopeContainsKeys(IEnumerable<IDictionary<string, object?>> scopes, params string[] keys)
    {
        if (scopes is null) throw new ArgumentNullException(nameof(scopes));
        if (keys is null || keys.Length == 0) return true;
        return scopes.Any(s => s != null && ContainsKeys(s, keys));
    }

    /// <summary>
    /// Throws InvalidOperationException when no scope contains the requested keys. Useful inside tests.
    /// </summary>
    public static void EnsureAnyScopeContainsKeys(IEnumerable<IDictionary<string, object?>> scopes, params string[] keys)
    {
        if (!AnyScopeContainsKeys(scopes, keys))
            throw new InvalidOperationException($"No scope contains required keys: {string.Join(",", keys)}");
    }
}
