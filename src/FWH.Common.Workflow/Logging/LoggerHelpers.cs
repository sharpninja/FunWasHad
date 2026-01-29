using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Logging;

public static class LoggerHelpers
{
    /// <summary>
    /// Resolve an ILogger&lt;T&gt; instance using the provided explicit logger, the service provider, or a NullLogger fallback.
    /// This centralizes the null-logger pattern so callers don't duplicate the same logic.
    /// </summary>
    public static ILogger<T> ResolveLogger<T>(IServiceProvider? serviceProvider, ILogger<T>? explicitLogger = null)
    {
        if (explicitLogger != null) return explicitLogger;
        if (serviceProvider != null)
        {
            try
            {
                var logger = serviceProvider.GetService<ILogger<T>>();
                if (logger != null) return logger;
            }
            catch
            {
                // ignore resolution errors and fall back to NullLogger
            }
        }

        return Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
    }
}
