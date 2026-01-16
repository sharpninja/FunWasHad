using System.Threading.Tasks;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow;

/// <summary>
/// Controller interface for workflow business logic operations.
/// Single Responsibility: Define workflow control operations.
/// </summary>
public interface IWorkflowController
{
    /// <summary>
    /// Import a workflow from PlantUML text.
    /// </summary>
    Task<WorkflowDefinition> ImportWorkflowAsync(string plantUmlText, string? id = null, string? name = null);

    /// <summary>
    /// Start or initialize a workflow instance.
    /// </summary>
    Task StartInstanceAsync(string workflowId);

    /// <summary>
    /// Restart a workflow instance from the beginning.
    /// </summary>
    Task RestartInstanceAsync(string workflowId);

    /// <summary>
    /// Get the current state payload for a workflow.
    /// </summary>
    Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId);

    /// <summary>
    /// Advance a workflow by choosing a value.
    /// </summary>
    Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue);

    /// <summary>
    /// Get the current node ID for a workflow instance.
    /// </summary>
    string? GetCurrentNodeId(string workflowId);

    /// <summary>
    /// Check if a workflow definition exists.
    /// </summary>
    bool WorkflowExists(string workflowId);
}
