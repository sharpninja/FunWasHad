using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FWH.Common.Workflow.Models;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow;

public record WorkflowChoiceOption(int Order, string DisplayText, string TargetNodeId, string? Condition = null);
public record WorkflowStatePayload(bool IsChoice, string? Text, IReadOnlyList<WorkflowChoiceOption> Choices);

/// <summary>
/// Service facade for workflow operations.
/// Delegates to IWorkflowController for business logic.
/// Single Responsibility: Provide service interface for workflow operations.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowController _controller;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowController controller,
        ILogger<WorkflowService>? logger = null)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowService>.Instance;
    }

    public async Task<WorkflowDefinition> ImportWorkflowAsync(string plantUmlText, string? id = null, string? name = null)
    {
        return await _controller.ImportWorkflowAsync(plantUmlText, id, name);
    }

    public async Task StartInstanceAsync(string workflowId)
    {
        await _controller.StartInstanceAsync(workflowId);
    }

    public async Task RestartInstanceAsync(string workflowId)
    {
        await _controller.RestartInstanceAsync(workflowId);
    }

    public Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId)
    {
        return _controller.GetCurrentStatePayloadAsync(workflowId);
    }

    public async Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue)
    {
        return await _controller.AdvanceByChoiceValueAsync(workflowId, choiceValue);
    }

    // Backwards-compatible sync wrappers
    public WorkflowDefinition ImportWorkflow(string plantUmlText, string? id = null, string? name = null)
        => ImportWorkflowAsync(plantUmlText, id, name).GetAwaiter().GetResult();

    public void StartInstance(string workflowId)
        => StartInstanceAsync(workflowId).GetAwaiter().GetResult();

    public void RestartInstance(string workflowId)
        => RestartInstanceAsync(workflowId).GetAwaiter().GetResult();

    public WorkflowStatePayload GetCurrentStatePayload(string workflowId)
        => GetCurrentStatePayloadAsync(workflowId).GetAwaiter().GetResult();

    public bool AdvanceByChoiceValue(string workflowId, object? choiceValue)
        => AdvanceByChoiceValueAsync(workflowId, choiceValue).GetAwaiter().GetResult();
}
