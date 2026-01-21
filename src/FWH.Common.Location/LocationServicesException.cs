using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FWH.Common.Location;

/// <summary>
/// Exception thrown when location services fail.
/// Includes detailed diagnostic information to help debug location service issues.
/// </summary>
public class LocationServicesException : Exception
{
    /// <summary>
    /// Gets the platform on which the error occurred.
    /// </summary>
    public string Platform { get; }

    /// <summary>
    /// Gets the operation that was being performed when the error occurred.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Gets additional diagnostic information about the failure.
    /// </summary>
    public Dictionary<string, object?> Diagnostics { get; }

    public LocationServicesException(
        string platform,
        string operation,
        string message,
        Dictionary<string, object?>? diagnostics = null,
        Exception? innerException = null)
        : base(BuildMessage(platform, operation, message, diagnostics), innerException)
    {
        Platform = platform;
        Operation = operation;
        Diagnostics = diagnostics ?? new Dictionary<string, object?>();
    }

    private static string BuildMessage(
        string platform,
        string operation,
        string message,
        Dictionary<string, object?>? diagnostics)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Location service failure on {platform} during {operation}: {message}");

        if (diagnostics != null && diagnostics.Count > 0)
        {
            sb.AppendLine("Diagnostics:");
            foreach (var kvp in diagnostics.OrderBy(k => k.Key))
            {
                var value = kvp.Value?.ToString() ?? "null";
                if (value.Length > 200)
                    value = value.Substring(0, 200) + "...";
                sb.AppendLine($"  {kvp.Key}: {value}");
            }
        }

        return sb.ToString();
    }
}
