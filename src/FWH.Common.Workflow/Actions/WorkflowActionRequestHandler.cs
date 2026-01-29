using FWH.Common.Workflow.Instance;
using FWH.Orchestrix.Contracts.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Actions;

public sealed partial class WorkflowActionRequestHandler : IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>
{
    [LoggerMessage(LogLevel.Warning, "No handler registered for action {ActionName}")]
    private static partial void LogNoHandlerRegistered(ILogger logger, string actionName);

    [LoggerMessage(LogLevel.Warning, "Handler factory returned null for action {ActionName}")]
    private static partial void LogHandlerFactoryReturnedNull(ILogger logger, string actionName);

    [LoggerMessage(LogLevel.Information, "Action {ActionName} handled by handler in {ElapsedMs}ms")]
    private static partial void LogActionHandled(ILogger logger, string actionName, long elapsedMs);

    [LoggerMessage(LogLevel.Information, "Action {ActionName} execution cancelled")]
    private static partial void LogActionCancelled(ILogger logger, string actionName);

    [LoggerMessage(LogLevel.Error, "Action handler for {ActionName} threw an exception")]
    private static partial void LogActionHandlerThrew(ILogger logger, Exception ex, string actionName);

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
            LogNoHandlerRegistered(_logger, request.ActionName);
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
                    LogHandlerFactoryReturnedNull(_logger, request.ActionName);
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
                    LogHandlerFactoryReturnedNull(_logger, request.ActionName);
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
                LogActionHandled(_logger, request.ActionName, sw.ElapsedMilliseconds);
            }

            return new WorkflowActionResponse
            {
                Success = true,
                VariableUpdates = updates
            };
        }
        catch (OperationCanceledException)
        {
            LogActionCancelled(_logger, request.ActionName);
            return new WorkflowActionResponse { Success = false, ErrorMessage = "Action execution was cancelled" };
        }
        catch (Exception ex)
        {
            LogActionHandlerThrew(_logger, ex, request.ActionName);
            // Return success=true even on exception - allows workflow to continue (matches direct handler path behavior)
            return new WorkflowActionResponse { Success = true, VariableUpdates = null };
        }
    }
}
