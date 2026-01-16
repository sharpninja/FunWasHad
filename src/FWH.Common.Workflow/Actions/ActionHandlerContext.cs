using System.Threading;
using FWH.Common.Workflow.Models;
using FWH.Common.Workflow.Instance;

namespace FWH.Common.Workflow.Actions;

public class ActionHandlerContext
{
    public string WorkflowId { get; }
    public WorkflowNode Node { get; }
    public WorkflowDefinition Definition { get; }
    public IWorkflowInstanceManager InstanceManager { get; }

    public ActionHandlerContext(string workflowId, WorkflowNode node, WorkflowDefinition definition, IWorkflowInstanceManager instanceManager)
    {
        WorkflowId = workflowId;
        Node = node;
        Definition = definition;
        InstanceManager = instanceManager;
    }
}
