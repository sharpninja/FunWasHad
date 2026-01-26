namespace FWH.Common.Workflow.Instance;

/// <summary>
/// Responsible for managing workflow instance state (current node tracking).
/// Single Responsibility: Track current node for each workflow instance.
/// </summary>
public interface IWorkflowInstanceManager
{
    /// <summary>
    /// Get the current node ID for a workflow instance.
    /// </summary>
    string? GetCurrentNode(string workflowId);

    /// <summary>
    /// Set the current node ID for a workflow instance.
    /// </summary>
    void SetCurrentNode(string workflowId, string? nodeId);

    /// <summary>
    /// Clear the current node for a workflow instance (for restart scenarios).
    /// </summary>
    void ClearCurrentNode(string workflowId);

    /// <summary>
    /// Get variables associated with the workflow instance (key/value strings).
    /// </summary>
    System.Collections.Generic.IDictionary<string, string>? GetVariables(string workflowId);

    /// <summary>
    /// Set a variable for the workflow instance.
    /// </summary>
    void SetVariable(string workflowId, string key, string value);
}
