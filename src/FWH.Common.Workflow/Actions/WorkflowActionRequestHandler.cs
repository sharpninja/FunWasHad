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
            var updates = await handler.HandleAsync(ctx, request.Parameters, cancellationToken).ConfigureAwait(false);

            return new WorkflowActionResponse
            {
                Success = true,
                VariableUpdates = updates
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation executing action {ActionName}", request.ActionName);
            return new WorkflowActionResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}
