using System.ComponentModel;

namespace FWH.Common.Workflow.Views;

/// <summary>
/// View interface for workflow interactions following MVVM pattern.
/// Maintains workflow state and notifies of changes.
/// Single Responsibility: Represent workflow view state and coordinate with controller.
/// </summary>
public interface IWorkflowView : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the current workflow ID being managed by this view.
    /// </summary>
    string? CurrentWorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the current node ID within the active workflow.
    /// </summary>
    string? CurrentNodeId { get; set; }

    /// <summary>
    /// Gets the current workflow state payload for UI rendering.
    /// </summary>
    WorkflowStatePayload? CurrentState { get; }

    /// <summary>
    /// Gets a value indicating whether the workflow is currently loading.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets a value indicating whether the workflow has an error.
    /// </summary>
    bool HasError { get; }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Load a workflow by ID and initialize the view state.
    /// </summary>
    Task LoadWorkflowAsync(string workflowId);

    /// <summary>
    /// Advance the workflow by making a choice.
    /// </summary>
    Task<bool> AdvanceAsync(object? choiceValue);

    /// <summary>
    /// Restart the current workflow from the beginning.
    /// </summary>
    Task RestartAsync();

    /// <summary>
    /// Refresh the current workflow state.
    /// </summary>
    Task RefreshStateAsync();
}
