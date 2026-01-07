using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Actions;

/// <summary>
/// On-demand registrar that wires handlers registered in DI into the registry.
/// This registrar supports handler factories to resolve handlers with scoped services.
/// </summary>
public class WorkflowActionHandlerRegistrar
{
    private readonly IWorkflowActionHandlerRegistry _registry;
    private readonly IServiceProvider _sp;
    private readonly ILogger<WorkflowActionHandlerRegistrar>? _logger;

    public WorkflowActionHandlerRegistrar(IServiceProvider sp, IWorkflowActionHandlerRegistry registry, ILogger<WorkflowActionHandlerRegistrar>? logger = null)
    {
        _sp = sp;
        _registry = registry;
        _logger = logger;

        // discover any IWorkflowActionHandler singletons and register factories for them
        var handlers = sp.GetServices<IWorkflowActionHandler>();
        foreach (var h in handlers)
        {
            _registry.Register(h.Name, _ => h);
            _logger?.LogInformation("Wired singleton handler {HandlerName} into registry", h.Name);
        }

        // discover any Func<IServiceProvider,IWorkflowActionHandler> factories registered in DI and register them
        var factories = sp.GetServices<Func<IServiceProvider, IWorkflowActionHandler>>();
        foreach (var f in factories)
        {
            try
            {
                // Use a scope when sampling factory in case it requires scoped services
                using var scope = sp.CreateScope();
                var sample = f(scope.ServiceProvider);
                // Register the factory so executor can create handler instances from a scope during execution
                _registry.Register(sample.Name, f);
                _logger?.LogInformation("Wired factory handler {HandlerName} into registry", sample.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Factory sampling failed for a handler factory; registering factory without sampling");
                try
                {
                    // Try to register the factory with a placeholder name if sampling failed; we attempt to instantiate later when executing
                    // Cannot determine name now, so skip registering here — factories should be registered alongside a singleton adapter or via AddWorkflowActionHandler<THandler>() which samples successfully
                }
                catch
                {
                    // swallow
                }
            }
        }
    }
}
