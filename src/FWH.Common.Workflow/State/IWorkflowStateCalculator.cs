using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow.State;

/// <summary>
/// Responsible for calculating workflow state and payloads.
/// Single Responsibility: Compute workflow state transitions and UI payloads.
/// </summary>
public interface IWorkflowStateCalculator
{
    /// <summary>
    /// Calculate the initial start node for a workflow, advancing through
    /// single transitions to reach the first meaningful node.
    /// </summary>
    string? CalculateStartNode(WorkflowDefinition definition);

    /// <summary>
    /// Calculate the current state payload for UI rendering.
    /// </summary>
    WorkflowStatePayload CalculateCurrentPayload(WorkflowDefinition definition, string? currentNodeId);
}
