using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Actions;

/// <summary>
/// On-demand registrar that wires handlers registered in DI into the registry.
/// This registrar supports handler factories to resolve handlers with scoped services.
/// </summary>
public partial class WorkflowActionHandlerRegistrar
{
    [LoggerMessage(LogLevel.Information, "Discovered {Count} singleton handlers during registration")]
    private static partial void LogDiscoveredSingletons(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Registered singleton handler '{HandlerName}' into registry")]
    private static partial void LogRegisteredSingleton(ILogger logger, string handlerName);

    [LoggerMessage(LogLevel.Information, "Discovered {Count} handler factories during registration")]
    private static partial void LogDiscoveredFactories(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Registered factory handler '{HandlerName}' into registry")]
    private static partial void LogRegisteredFactoryHandler(ILogger logger, string handlerName);

    [LoggerMessage(LogLevel.Warning, "Factory sampling failed for a handler factory; skipping registration")]
    private static partial void LogFactorySamplingFailed(ILogger logger, Exception ex);

    private readonly IWorkflowActionHandlerRegistry _registry;
    private readonly IServiceProvider _sp;
    private readonly ILogger<WorkflowActionHandlerRegistrar>? _logger;

    public WorkflowActionHandlerRegistrar(IServiceProvider sp, IWorkflowActionHandlerRegistry registry, ILogger<WorkflowActionHandlerRegistrar>? logger = null)
    {
        _sp = sp;
        _registry = registry;
        _logger = logger;

        // Discover any IWorkflowActionHandler singletons and register factories for them
        var handlers = sp.GetServices<IWorkflowActionHandler>().ToList();
        if (_logger != null) LogDiscoveredSingletons(_logger, handlers.Count);

        foreach (var h in handlers)
        {
            _registry.Register(h.Name, _ => h);
            if (_logger != null) LogRegisteredSingleton(_logger, h.Name);
        }

        // Discover any Func<IServiceProvider,IWorkflowActionHandler> factories registered in DI and register them
        var factories = sp.GetServices<Func<IServiceProvider, IWorkflowActionHandler>>().ToList();
        if (_logger != null) LogDiscoveredFactories(_logger, factories.Count);

        foreach (var f in factories)
        {
            try
            {
                // Use a scope when sampling factory in case it requires scoped services
                using var scope = sp.CreateScope();
                var sample = f(scope.ServiceProvider);
                // Register the factory so executor can create handler instances from a scope during execution
                _registry.Register(sample.Name, f);
                if (_logger != null) LogRegisteredFactoryHandler(_logger, sample.Name);
            }
            catch (Exception ex)
            {
                if (_logger != null) LogFactorySamplingFailed(_logger, ex);
            }
        }
    }
}
