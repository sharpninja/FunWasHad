using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow.Storage;

/// <summary>
/// Responsible for storing and retrieving workflow definitions.
/// Single Responsibility: Manage workflow definition storage.
/// </summary>
public interface IWorkflowDefinitionStore
{
    /// <summary>
    /// Store a workflow definition.
    /// </summary>
    void Store(WorkflowDefinition definition);

    /// <summary>
    /// Retrieve a workflow definition by ID.
    /// </summary>
    WorkflowDefinition? Get(string workflowId);

    /// <summary>
    /// Check if a workflow definition exists.
    /// </summary>
    bool Exists(string workflowId);
}
