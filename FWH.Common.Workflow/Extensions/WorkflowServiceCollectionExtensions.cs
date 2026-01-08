using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Logging;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace FWH.Common.Workflow.Extensions;

/// <summary>
/// Extension methods for registering workflow services with dependency injection.
/// Single Responsibility: Configure DI for workflow components.
/// </summary>
public static class WorkflowServiceCollectionExtensions
{
    /// <summary>
    /// Adds all workflow services to the service collection.
    /// Includes definition store, instance manager, mapper, state calculator, controller, service, and view.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowServices(this IServiceCollection services, WorkflowActionExecutorOptions? executorOptions = null)
    {
        // Logging infrastructure
        services.AddSingleton<ICorrelationIdService, CorrelationIdService>();

        // Core workflow components (SRP-compliant with Controller pattern)
        services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();

        // Handler registry and executor
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<WorkflowActionHandlerRegistrar>();

        var opts = executorOptions ?? new WorkflowActionExecutorOptions();
        services.AddSingleton<IWorkflowActionExecutor>(sp => new WorkflowActionExecutor(sp, sp.GetRequiredService<IWorkflowActionHandlerRegistry>(), Options.Create(opts), sp.GetService<Microsoft.Extensions.Logging.ILogger<WorkflowActionExecutor>>()));

        // Controller and service
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        
        // View (transient for multiple workflow instances)
        services.AddTransient<IWorkflowView, WorkflowView>();

        return services;
    }

    /// <summary>
    /// Register an action handler instance with a name.
    /// </summary>
    public static IServiceCollection AddWorkflowActionHandler(this IServiceCollection services, IWorkflowActionHandler handler)
    {
        // Register the handler as a singleton so it will be discovered by the registrar
        services.AddSingleton<IWorkflowActionHandler>(handler);
        services.AddSingleton(handler.GetType(), handler);
        return services;
    }

    /// <summary>
    /// Register an action handler delegate by name. Convenience overload.
    /// </summary>
    public static IServiceCollection AddWorkflowActionHandler(this IServiceCollection services, string name, System.Func<ActionHandlerContext, System.Collections.Generic.IDictionary<string,string>, System.Threading.CancellationToken, System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<string,string>?>> handler)
    {
        var adapter = new WorkflowActionHandlerAdapter(name, handler);
        services.AddWorkflowActionHandler(adapter);
        return services;
    }

    /// <summary>
    /// Register an action handler type (scoped) where THandler implements IWorkflowActionHandler.
    /// The registry will register a factory that resolves the handler from the IServiceProvider so scoped services can be used.
    /// </summary>
    public static IServiceCollection AddWorkflowActionHandler<THandler>(this IServiceCollection services)
        where THandler : class, IWorkflowActionHandler
    {
        services.AddScoped<THandler>();
        // Register factory with registry at startup via IServiceProvider; we store a descriptor in DI so registrar can discover it.
        services.AddSingleton<Func<IServiceProvider, IWorkflowActionHandler>>(sp => (provider) => provider.GetRequiredService<THandler>());
        // We also register a small marker to allow registrar to pick it up: register a factory that registrar can find via GetServices<Func<IServiceProvider, IWorkflowActionHandler>>()
        return services;
    }

    /// <summary>
    /// Adds workflow definition store to the service collection.
    /// </summary>
    /// <typeparam name="TStore">The implementation type for the definition store.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowDefinitionStore<TStore>(this IServiceCollection services)
        where TStore : class, IWorkflowDefinitionStore
    {
        services.AddSingleton<IWorkflowDefinitionStore, TStore>();
        return services;
    }

    /// <summary>
    /// Adds workflow instance manager to the service collection.
    /// </summary>
    /// <typeparam name="TManager">The implementation type for the instance manager.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowInstanceManager<TManager>(this IServiceCollection services)
        where TManager : class, IWorkflowInstanceManager
    {
        services.AddSingleton<IWorkflowInstanceManager, TManager>();
        return services;
    }
}
