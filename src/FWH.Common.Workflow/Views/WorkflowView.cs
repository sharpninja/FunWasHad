using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Views;

/// <summary>
/// View implementation for workflow interactions.
/// Coordinates with WorkflowController to manage workflow state.
/// Single Responsibility: Manage workflow view state and notify observers of changes.
/// </summary>
public partial class WorkflowView : IWorkflowView
{
    [LoggerMessage(LogLevel.Information, "Loaded workflow {WorkflowId}")]
    private static partial void LogLoadedWorkflow(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Failed to load workflow {WorkflowId}")]
    private static partial void LogLoadWorkflowFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Advanced workflow {WorkflowId} with choice {Choice}")]
    private static partial void LogAdvancedWorkflow(ILogger logger, string workflowId, object? choice);

    [LoggerMessage(LogLevel.Error, "Failed to advance workflow {WorkflowId}")]
    private static partial void LogAdvanceFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Information, "Restarted workflow {WorkflowId}")]
    private static partial void LogRestartedWorkflow(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Failed to restart workflow {WorkflowId}")]
    private static partial void LogRestartFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Error, "Failed to refresh state for workflow {WorkflowId}")]
    private static partial void LogRefreshStateFailed(ILogger logger, Exception ex, string workflowId);

    private readonly IWorkflowController _controller;
    private readonly ILogger<WorkflowView>? _logger;

    private string? _currentWorkflowId;
    private string? _currentNodeId;
    private WorkflowStatePayload? _currentState;
    private bool _isLoading;
    private bool _hasError;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public WorkflowView(IWorkflowController controller, ILogger<WorkflowView>? logger = null)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _logger = logger;
    }

    public string? CurrentWorkflowId
    {
        get => _currentWorkflowId;
        set => SetProperty(ref _currentWorkflowId, value);
    }

    public string? CurrentNodeId
    {
        get => _currentNodeId;
        set => SetProperty(ref _currentNodeId, value);
    }

    public WorkflowStatePayload? CurrentState
    {
        get => _currentState;
        private set => SetProperty(ref _currentState, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public async Task LoadWorkflowAsync(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            CurrentWorkflowId = workflowId;
            await _controller.StartInstanceAsync(workflowId).ConfigureAwait(false);
            await RefreshStateAsync().ConfigureAwait(false);

            if (_logger != null) LogLoadedWorkflow(_logger, workflowId);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            if (_logger != null) LogLoadWorkflowFailed(_logger, ex, workflowId);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> AdvanceAsync(object? choiceValue)
    {
        if (string.IsNullOrWhiteSpace(CurrentWorkflowId))
            throw new InvalidOperationException("No workflow loaded");

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            var advanced = await _controller.AdvanceByChoiceValueAsync(CurrentWorkflowId, choiceValue).ConfigureAwait(false);

            if (advanced)
            {
                await RefreshStateAsync().ConfigureAwait(false);
                if (_logger != null) LogAdvancedWorkflow(_logger, CurrentWorkflowId, choiceValue?.ToString() ?? "null");
            }

            return advanced;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            if (_logger != null) LogAdvanceFailed(_logger, ex, CurrentWorkflowId);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RestartAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentWorkflowId))
            throw new InvalidOperationException("No workflow loaded");

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            await _controller.RestartInstanceAsync(CurrentWorkflowId).ConfigureAwait(false);
            await RefreshStateAsync().ConfigureAwait(false);

            if (_logger != null) LogRestartedWorkflow(_logger, CurrentWorkflowId);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            if (_logger != null) LogRestartFailed(_logger, ex, CurrentWorkflowId);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshStateAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentWorkflowId))
            return;

        try
        {
            var state = await _controller.GetCurrentStatePayloadAsync(CurrentWorkflowId).ConfigureAwait(false);
            CurrentState = state;

            // Update current node from controller
            CurrentNodeId = _controller.GetCurrentNodeId(CurrentWorkflowId);

            OnPropertyChanged(nameof(CurrentState));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            if (_logger != null) LogRefreshStateFailed(_logger, ex, CurrentWorkflowId);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
