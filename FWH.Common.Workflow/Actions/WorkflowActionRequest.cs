using FWH.Common.Workflow.Models;
using Orchestrix.Contracts.Mediator;

namespace FWH.Common.Workflow.Actions;

public sealed class WorkflowActionRequest : IMediatorRequest<WorkflowActionResponse>
{
    public string WorkflowId { get; set; } = string.Empty;
    public WorkflowNode Node { get; set; } = null!;
    public WorkflowDefinition Definition { get; set; } = null!;
    public string ActionName { get; set; } = string.Empty;
    public IDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
}

public sealed class WorkflowActionResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IDictionary<string, string>? VariableUpdates { get; set; }
}
