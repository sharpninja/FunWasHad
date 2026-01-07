using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Views;

/// <summary>
/// View implementation for workflow interactions.
/// Coordinates with WorkflowController to manage workflow state.
/// Single Responsibility: Manage workflow view state and notify observers of changes.
/// </summary>
public class WorkflowView : IWorkflowView
{
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
            await _controller.StartInstanceAsync(workflowId);
            await RefreshStateAsync();

            _logger?.LogInformation("Loaded workflow {WorkflowId}", workflowId);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _logger?.LogError(ex, "Failed to load workflow {WorkflowId}", workflowId);
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

            var advanced = await _controller.AdvanceByChoiceValueAsync(CurrentWorkflowId, choiceValue);
            
            if (advanced)
            {
                await RefreshStateAsync();
                _logger?.LogDebug("Advanced workflow {WorkflowId} with choice {Choice}", 
                    CurrentWorkflowId, choiceValue);
            }

            return advanced;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _logger?.LogError(ex, "Failed to advance workflow {WorkflowId}", CurrentWorkflowId);
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

            await _controller.RestartInstanceAsync(CurrentWorkflowId);
            await RefreshStateAsync();

            _logger?.LogInformation("Restarted workflow {WorkflowId}", CurrentWorkflowId);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _logger?.LogError(ex, "Failed to restart workflow {WorkflowId}", CurrentWorkflowId);
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
            var state = await _controller.GetCurrentStatePayloadAsync(CurrentWorkflowId);
            CurrentState = state;
            
            // Update current node from controller
            CurrentNodeId = _controller.GetCurrentNodeId(CurrentWorkflowId);
            
            OnPropertyChanged(nameof(CurrentState));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _logger?.LogError(ex, "Failed to refresh state for workflow {WorkflowId}", CurrentWorkflowId);
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
