using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FWH.Common.Workflow.Instance;
using FWH.Orchestrix.Contracts.Mediator;

namespace FWH.Common.Workflow.Actions;

public sealed class WorkflowActionRequestHandler : IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowActionHandlerRegistry _registry;
    private readonly ILogger<WorkflowActionRequestHandler> _logger;

    public WorkflowActionRequestHandler(
        IServiceProvider serviceProvider,
        IWorkflowActionHandlerRegistry registry,
        ILogger<WorkflowActionRequestHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _registry = registry;
        _logger = logger;
    }

    public async Task<WorkflowActionResponse> HandleAsync(WorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ActionName))
        {
            return new WorkflowActionResponse { Success = false, ErrorMessage = "ActionName is required." };
        }

        if (!_registry.TryGetFactory(request.ActionName, out var factory) || factory is null)
        {
            _logger.LogWarning("No handler registered for action {ActionName}", request.ActionName);
            return new WorkflowActionResponse { Success = false, ErrorMessage = $"No handler for {request.ActionName}" };
        }

        try
        {
            IDictionary<string, string>? updates;
            System.Diagnostics.Stopwatch? sw = null;

            if (request.LogExecutionTime)
            {
                sw = System.Diagnostics.Stopwatch.StartNew();
            }

            if (request.CreateScopeForHandlers)
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = factory(scope.ServiceProvider);
                if (handler is null)
                {
                    _logger.LogWarning("Handler factory returned null for action {ActionName}", request.ActionName);
                    return new WorkflowActionResponse { Success = false, ErrorMessage = $"Handler factory returned null for {request.ActionName}" };
                }

                var instanceManager = scope.ServiceProvider.GetService<IWorkflowInstanceManager>();
                if (instanceManager is null)
                {
                    throw new InvalidOperationException("InstanceManager required in workflow action scope.");
                }

                var ctx = new ActionHandlerContext(request.WorkflowId, request.Node, request.Definition, instanceManager);
                updates = await handler.HandleAsync(ctx, request.Parameters, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Use root service provider without creating a scope
                var handler = factory(_serviceProvider);
                if (handler is null)
                {
                    _logger.LogWarning("Handler factory returned null for action {ActionName}", request.ActionName);
                    return new WorkflowActionResponse { Success = false, ErrorMessage = $"Handler factory returned null for {request.ActionName}" };
                }

                var instanceManager = _serviceProvider.GetService<IWorkflowInstanceManager>();
                if (instanceManager is null)
                {
                    throw new InvalidOperationException("InstanceManager required in workflow action scope.");
                }

                var ctx = new ActionHandlerContext(request.WorkflowId, request.Node, request.Definition, instanceManager);
                updates = await handler.HandleAsync(ctx, request.Parameters, cancellationToken).ConfigureAwait(false);
            }

            if (sw != null)
            {
                sw.Stop();
                _logger.LogInformation("Action {ActionName} handled by handler in {ElapsedMs}ms", request.ActionName, sw.ElapsedMilliseconds);
            }

            return new WorkflowActionResponse
            {
                Success = true,
                VariableUpdates = updates
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Action {ActionName} execution cancelled", request.ActionName);
            return new WorkflowActionResponse { Success = false, ErrorMessage = "Action execution was cancelled" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action handler for {ActionName} threw an exception", request.ActionName);
            // Return success=true even on exception - allows workflow to continue (matches direct handler path behavior)
            return new WorkflowActionResponse { Success = true, VariableUpdates = null };
        }
    }
}
