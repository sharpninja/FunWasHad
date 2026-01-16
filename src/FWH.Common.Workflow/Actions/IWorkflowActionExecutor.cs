using System.Threading;
using System.Threading.Tasks;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow.Actions;

public interface IWorkflowActionExecutor
{
    /// <summary>
    /// Execute an action definition JSON string associated with a workflow node.
    /// Returns true if execution succeeded.
    /// </summary>
    Task<bool> ExecuteAsync(string workflowId, WorkflowNode node, WorkflowDefinition definition, CancellationToken cancellationToken = default);
}
