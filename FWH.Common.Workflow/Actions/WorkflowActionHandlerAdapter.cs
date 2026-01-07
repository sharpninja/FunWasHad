using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Common.Workflow.Actions;

/// <summary>
/// Simple adapter to register static handler delegates as IWorkflowActionHandler.
/// Supports delegates that return typed results or synchronous handlers.
/// </summary>
public class WorkflowActionHandlerAdapter : IWorkflowActionHandler
{
    public string Name { get; }
    private readonly Func<ActionHandlerContext, IDictionary<string,string>, CancellationToken, Task<IDictionary<string,string>?>> _handler;

    public WorkflowActionHandlerAdapter(string name, Func<ActionHandlerContext, IDictionary<string,string>, CancellationToken, Task<IDictionary<string,string>?>> handler)
    {
        Name = name;
        _handler = handler;
    }

    public Task<IDictionary<string,string>?> HandleAsync(ActionHandlerContext context, IDictionary<string,string> parameters, CancellationToken cancellationToken = default) => _handler(context, parameters, cancellationToken);

    // Convenience constructor for delegate that ignores context and cancellation
    public WorkflowActionHandlerAdapter(string name, Func<IDictionary<string,string>, Task<IDictionary<string,string>?>> handler)
        : this(name, (ctx, p, ct) => handler(p)) { }

    // Convenience constructor for sync delegate
    public WorkflowActionHandlerAdapter(string name, Func<IDictionary<string,string>, IDictionary<string,string>?> handler)
        : this(name, (ctx, p, ct) => Task.FromResult(handler(p))) { }

    // Convenience constructor for delegate that only uses context and parameters and returns typed results other than string map
    public static WorkflowActionHandlerAdapter FromTyped<T>(string name, Func<ActionHandlerContext, IDictionary<string,string>, CancellationToken, Task<T>> typedHandler, Func<T, IDictionary<string,string>?> resultMapper)
    {
        return new WorkflowActionHandlerAdapter(name, async (ctx, p, ct) =>
        {
            var typed = await typedHandler(ctx, p, ct);
            return resultMapper(typed);
        });
    }
}
