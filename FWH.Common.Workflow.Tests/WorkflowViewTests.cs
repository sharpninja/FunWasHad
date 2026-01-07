using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Models;
using System.Collections.Generic;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Unit tests for WorkflowView following MVVM pattern with INotifyPropertyChanged.
/// </summary>
public class WorkflowViewTests
{
    private readonly Mock<IWorkflowController> _mockController;
    private readonly Mock<ILogger<WorkflowView>> _mockLogger;
    private readonly WorkflowView _view;

    public WorkflowViewTests()
    {
        _mockController = new Mock<IWorkflowController>();
        _mockLogger = new Mock<ILogger<WorkflowView>>();
        _view = new WorkflowView(_mockController.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidController_CreatesInstance()
    {
        // Arrange & Act
        var view = new WorkflowView(_mockController.Object);

        // Assert
        Assert.NotNull(view);
        Assert.Null(view.CurrentWorkflowId);
        Assert.Null(view.CurrentNodeId);
        Assert.Null(view.CurrentState);
        Assert.False(view.IsLoading);
        Assert.False(view.HasError);
        Assert.Null(view.ErrorMessage);
    }

    [Fact]
    public void Constructor_WithNullController_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WorkflowView(null!));
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void CurrentWorkflowId_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        string? changedPropertyName = null;
        _view.PropertyChanged += (s, e) => changedPropertyName = e.PropertyName;

        // Act
        _view.CurrentWorkflowId = "test-workflow";

        // Assert
        Assert.Equal("test-workflow", _view.CurrentWorkflowId);
        Assert.Equal(nameof(_view.CurrentWorkflowId), changedPropertyName);
    }

    [Fact]
    public void CurrentNodeId_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        string? changedPropertyName = null;
        _view.PropertyChanged += (s, e) => changedPropertyName = e.PropertyName;

        // Act
        _view.CurrentNodeId = "node-1";

        // Assert
        Assert.Equal("node-1", _view.CurrentNodeId);
        Assert.Equal(nameof(_view.CurrentNodeId), changedPropertyName);
    }

    [Fact]
    public void PropertyChanged_WithSameValue_DoesNotRaiseEvent()
    {
        // Arrange
        _view.CurrentWorkflowId = "test-workflow";
        int eventCount = 0;
        _view.PropertyChanged += (s, e) => eventCount++;

        // Act
        _view.CurrentWorkflowId = "test-workflow";

        // Assert
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void PropertyChanged_WithMultipleSubscribers_NotifiesAll()
    {
        // Arrange
        int subscriber1Count = 0;
        int subscriber2Count = 0;
        _view.PropertyChanged += (s, e) => subscriber1Count++;
        _view.PropertyChanged += (s, e) => subscriber2Count++;

        // Act
        _view.CurrentWorkflowId = "test";
        _view.CurrentNodeId = "node";

        // Assert
        Assert.Equal(2, subscriber1Count);
        Assert.Equal(2, subscriber2Count);
    }

    #endregion

    #region LoadWorkflowAsync Tests

    [Fact]
    public async Task LoadWorkflowAsync_WithValidWorkflowId_LoadsSuccessfully()
    {
        // Arrange
        var workflowId = "test-workflow";
        var expectedPayload = new WorkflowStatePayload(false, "Test", Array.Empty<WorkflowChoiceOption>());
        
        _mockController.Setup(c => c.StartInstanceAsync(workflowId))
            .Returns(Task.CompletedTask);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(expectedPayload);
        _mockController.Setup(c => c.GetCurrentNodeId(workflowId))
            .Returns("node-1");

        // Act
        await _view.LoadWorkflowAsync(workflowId);

        // Assert
        Assert.Equal(workflowId, _view.CurrentWorkflowId);
        Assert.Equal("node-1", _view.CurrentNodeId);
        Assert.Equal(expectedPayload, _view.CurrentState);
        Assert.False(_view.IsLoading);
        Assert.False(_view.HasError);
        _mockController.Verify(c => c.StartInstanceAsync(workflowId), Times.Once);
    }

    [Fact]
    public async Task LoadWorkflowAsync_SetsIsLoadingDuringExecution()
    {
        // Arrange
        var workflowId = "test-workflow";
        bool wasLoading = false;
        
        _mockController.Setup(c => c.StartInstanceAsync(workflowId))
            .Callback(() => wasLoading = _view.IsLoading)
            .Returns(Task.CompletedTask);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "", Array.Empty<WorkflowChoiceOption>()));

        // Act
        await _view.LoadWorkflowAsync(workflowId);

        // Assert
        Assert.True(wasLoading);
        Assert.False(_view.IsLoading);
    }

    [Fact]
    public async Task LoadWorkflowAsync_WithNullWorkflowId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _view.LoadWorkflowAsync(null!));
    }

    [Fact]
    public async Task LoadWorkflowAsync_WithEmptyWorkflowId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _view.LoadWorkflowAsync(string.Empty));
    }

    [Fact]
    public async Task LoadWorkflowAsync_WhenControllerThrows_SetsErrorState()
    {
        // Arrange
        var workflowId = "test-workflow";
        var exception = new InvalidOperationException("Test error");
        
        _mockController.Setup(c => c.StartInstanceAsync(workflowId))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _view.LoadWorkflowAsync(workflowId));
        
        Assert.True(_view.HasError);
        Assert.Equal("Test error", _view.ErrorMessage);
        Assert.False(_view.IsLoading);
    }

    [Fact]
    public async Task LoadWorkflowAsync_RaisesPropertyChangedForAllProperties()
    {
        // Arrange
        var workflowId = "test-workflow";
        var changedProperties = new List<string>();
        
        _mockController.Setup(c => c.StartInstanceAsync(workflowId))
            .Returns(Task.CompletedTask);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "", Array.Empty<WorkflowChoiceOption>()));
        
        _view.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        await _view.LoadWorkflowAsync(workflowId);

        // Assert
        Assert.Contains(nameof(_view.IsLoading), changedProperties);
        Assert.Contains(nameof(_view.CurrentWorkflowId), changedProperties);
        Assert.Contains(nameof(_view.CurrentState), changedProperties);
    }

    #endregion

    #region AdvanceAsync Tests

    [Fact]
    public async Task AdvanceAsync_WithValidChoice_AdvancesSuccessfully()
    {
        // Arrange
        var workflowId = "test-workflow";
        var choiceValue = "choice-1";
        var newPayload = new WorkflowStatePayload(false, "Advanced", Array.Empty<WorkflowChoiceOption>());
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.AdvanceByChoiceValueAsync(workflowId, choiceValue))
            .ReturnsAsync(true);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(newPayload);
        _mockController.Setup(c => c.GetCurrentNodeId(workflowId))
            .Returns("node-2");

        // Act
        var result = await _view.AdvanceAsync(choiceValue);

        // Assert
        Assert.True(result);
        Assert.Equal(newPayload, _view.CurrentState);
        Assert.Equal("node-2", _view.CurrentNodeId);
        Assert.False(_view.HasError);
        _mockController.Verify(c => c.AdvanceByChoiceValueAsync(workflowId, choiceValue), Times.Once);
    }

    [Fact]
    public async Task AdvanceAsync_WhenNoWorkflowLoaded_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _view.AdvanceAsync("test"));
    }

    [Fact]
    public async Task AdvanceAsync_WhenControllerReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var workflowId = "test-workflow";
        _view.CurrentWorkflowId = workflowId;
        
        _mockController.Setup(c => c.AdvanceByChoiceValueAsync(workflowId, It.IsAny<object>()))
            .ReturnsAsync(false);

        // Act
        var result = await _view.AdvanceAsync("invalid-choice");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AdvanceAsync_SetsIsLoadingDuringExecution()
    {
        // Arrange
        var workflowId = "test-workflow";
        bool wasLoading = false;
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.AdvanceByChoiceValueAsync(workflowId, It.IsAny<object>()))
            .Callback(() => wasLoading = _view.IsLoading)
            .ReturnsAsync(true);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "", Array.Empty<WorkflowChoiceOption>()));

        // Act
        await _view.AdvanceAsync("choice");

        // Assert
        Assert.True(wasLoading);
        Assert.False(_view.IsLoading);
    }

    [Fact]
    public async Task AdvanceAsync_WhenControllerThrows_SetsErrorState()
    {
        // Arrange
        var workflowId = "test-workflow";
        var exception = new InvalidOperationException("Advance error");
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.AdvanceByChoiceValueAsync(workflowId, It.IsAny<object>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _view.AdvanceAsync("choice"));
        
        Assert.True(_view.HasError);
        Assert.Equal("Advance error", _view.ErrorMessage);
    }

    [Fact]
    public async Task AdvanceAsync_WithNullChoice_CallsController()
    {
        // Arrange
        var workflowId = "test-workflow";
        _view.CurrentWorkflowId = workflowId;
        
        _mockController.Setup(c => c.AdvanceByChoiceValueAsync(workflowId, null))
            .ReturnsAsync(true);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "", Array.Empty<WorkflowChoiceOption>()));

        // Act
        var result = await _view.AdvanceAsync(null);

        // Assert
        Assert.True(result);
        _mockController.Verify(c => c.AdvanceByChoiceValueAsync(workflowId, null), Times.Once);
    }

    #endregion

    #region RestartAsync Tests

    [Fact]
    public async Task RestartAsync_WithLoadedWorkflow_RestartsSuccessfully()
    {
        // Arrange
        var workflowId = "test-workflow";
        var restartPayload = new WorkflowStatePayload(false, "Restarted", Array.Empty<WorkflowChoiceOption>());
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.RestartInstanceAsync(workflowId))
            .Returns(Task.CompletedTask);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(restartPayload);
        _mockController.Setup(c => c.GetCurrentNodeId(workflowId))
            .Returns("start-node");

        // Act
        await _view.RestartAsync();

        // Assert
        Assert.Equal(restartPayload, _view.CurrentState);
        Assert.Equal("start-node", _view.CurrentNodeId);
        Assert.False(_view.HasError);
        _mockController.Verify(c => c.RestartInstanceAsync(workflowId), Times.Once);
    }

    [Fact]
    public async Task RestartAsync_WhenNoWorkflowLoaded_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _view.RestartAsync());
    }

    [Fact]
    public async Task RestartAsync_SetsIsLoadingDuringExecution()
    {
        // Arrange
        var workflowId = "test-workflow";
        bool wasLoading = false;
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.RestartInstanceAsync(workflowId))
            .Callback(() => wasLoading = _view.IsLoading)
            .Returns(Task.CompletedTask);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "", Array.Empty<WorkflowChoiceOption>()));

        // Act
        await _view.RestartAsync();

        // Assert
        Assert.True(wasLoading);
        Assert.False(_view.IsLoading);
    }

    [Fact]
    public async Task RestartAsync_WhenControllerThrows_SetsErrorState()
    {
        // Arrange
        var workflowId = "test-workflow";
        var exception = new InvalidOperationException("Restart error");
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.RestartInstanceAsync(workflowId))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _view.RestartAsync());
        
        Assert.True(_view.HasError);
        Assert.Equal("Restart error", _view.ErrorMessage);
    }

    [Fact]
    public async Task RestartAsync_ClearsErrorState_BeforeOperation()
    {
        // Arrange
        var workflowId = "test-workflow";
        _view.CurrentWorkflowId = workflowId;
        
        // Set error state by making controller throw on advance
        _mockController.Setup(c => c.AdvanceByChoiceValueAsync(workflowId, "bad"))
            .ThrowsAsync(new InvalidOperationException("Test error"));
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => _view.AdvanceAsync("bad"));
        Assert.True(_view.HasError);
        
        // Setup for successful restart
        _mockController.Setup(c => c.RestartInstanceAsync(workflowId))
            .Returns(Task.CompletedTask);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "", Array.Empty<WorkflowChoiceOption>()));
        _mockController.Setup(c => c.GetCurrentNodeId(workflowId))
            .Returns("start");

        // Act
        await _view.RestartAsync();

        // Assert
        Assert.False(_view.HasError);
        Assert.Null(_view.ErrorMessage);
    }

    #endregion

    #region RefreshStateAsync Tests

    [Fact]
    public async Task RefreshStateAsync_WithLoadedWorkflow_RefreshesState()
    {
        // Arrange
        var workflowId = "test-workflow";
        var newPayload = new WorkflowStatePayload(true, "Choice", new[]
        {
            new WorkflowChoiceOption(0, "Option 1", "node-1")
        });
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(newPayload);
        _mockController.Setup(c => c.GetCurrentNodeId(workflowId))
            .Returns("node-3");

        // Act
        await _view.RefreshStateAsync();

        // Assert
        Assert.Equal(newPayload, _view.CurrentState);
        Assert.Equal("node-3", _view.CurrentNodeId);
    }

    [Fact]
    public async Task RefreshStateAsync_WithNoWorkflowLoaded_DoesNothing()
    {
        // Act
        await _view.RefreshStateAsync();

        // Assert
        Assert.Null(_view.CurrentState);
        _mockController.Verify(c => c.GetCurrentStatePayloadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshStateAsync_WhenControllerThrows_SetsErrorState()
    {
        // Arrange
        var workflowId = "test-workflow";
        var exception = new InvalidOperationException("Refresh error");
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ThrowsAsync(exception);

        // Act
        await _view.RefreshStateAsync();

        // Assert
        Assert.True(_view.HasError);
        Assert.Equal("Refresh error", _view.ErrorMessage);
    }

    [Fact]
    public async Task RefreshStateAsync_RaisesPropertyChangedForCurrentState()
    {
        // Arrange
        var workflowId = "test-workflow";
        var changedProperties = new List<string>();
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "", Array.Empty<WorkflowChoiceOption>()));
        _mockController.Setup(c => c.GetCurrentNodeId(workflowId))
            .Returns("node-1");
        
        _view.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act
        await _view.RefreshStateAsync();

        // Assert
        Assert.Contains(nameof(_view.CurrentState), changedProperties);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task FullWorkflow_LoadAdvanceRestart_WorksCorrectly()
    {
        // Arrange
        var workflowId = "full-test";
        var initialPayload = new WorkflowStatePayload(true, "Start", new[]
        {
            new WorkflowChoiceOption(0, "Go", "node-2")
        });
        var advancedPayload = new WorkflowStatePayload(false, "Node 2", Array.Empty<WorkflowChoiceOption>());
        
        _mockController.Setup(c => c.StartInstanceAsync(workflowId))
            .Returns(Task.CompletedTask);
        _mockController.SetupSequence(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(initialPayload)
            .ReturnsAsync(advancedPayload)
            .ReturnsAsync(initialPayload);
        _mockController.SetupSequence(c => c.GetCurrentNodeId(workflowId))
            .Returns("node-1")
            .Returns("node-2")
            .Returns("node-1");
        _mockController.Setup(c => c.AdvanceByChoiceValueAsync(workflowId, "Go"))
            .ReturnsAsync(true);
        _mockController.Setup(c => c.RestartInstanceAsync(workflowId))
            .Returns(Task.CompletedTask);

        // Act & Assert
        // Load
        await _view.LoadWorkflowAsync(workflowId);
        Assert.Equal(workflowId, _view.CurrentWorkflowId);
        Assert.True(_view.CurrentState!.IsChoice);

        // Advance
        var advanced = await _view.AdvanceAsync("Go");
        Assert.True(advanced);
        Assert.Equal("node-2", _view.CurrentNodeId);

        // Restart
        await _view.RestartAsync();
        Assert.Equal("node-1", _view.CurrentNodeId);
        Assert.True(_view.CurrentState!.IsChoice);
    }

    [Fact]
    public async Task ErrorRecovery_AfterError_CanPerformOperations()
    {
        // Arrange
        var workflowId = "error-recovery";
        
        _view.CurrentWorkflowId = workflowId;
        _mockController.SetupSequence(c => c.AdvanceByChoiceValueAsync(workflowId, It.IsAny<object>()))
            .ThrowsAsync(new InvalidOperationException("First error"))
            .ReturnsAsync(true);
        _mockController.Setup(c => c.GetCurrentStatePayloadAsync(workflowId))
            .ReturnsAsync(new WorkflowStatePayload(false, "Success", Array.Empty<WorkflowChoiceOption>()));

        // Act & Assert
        // First attempt fails
        await Assert.ThrowsAsync<InvalidOperationException>(() => _view.AdvanceAsync("choice"));
        Assert.True(_view.HasError);

        // Second attempt succeeds
        var result = await _view.AdvanceAsync("choice");
        Assert.True(result);
        Assert.False(_view.HasError);
    }

    #endregion
}
