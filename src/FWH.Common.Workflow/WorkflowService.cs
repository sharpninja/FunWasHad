using System.Diagnostics;
using FWH.Common.Workflow.Logging;
using FWH.Common.Workflow.Models;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow;

public record WorkflowChoiceOption(int Order, string DisplayText, string TargetNodeId, string? Condition = null);
public record WorkflowStatePayload(bool IsChoice, string? Text, IReadOnlyList<WorkflowChoiceOption> Choices, string? NodeLabel = null);

/// <summary>
/// Service facade for workflow operations.
/// Delegates to IWorkflowController for business logic.
/// Single Responsibility: Provide service interface for workflow operations.
/// </summary>
public partial class WorkflowService : IWorkflowService
{
    [LoggerMessage(LogLevel.Information, "Started instance for workflow {WorkflowId}")]
    private static partial void LogStartedInstance(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Failed to start instance for workflow {WorkflowId}")]
    private static partial void LogStartInstanceFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Information, "Restarted instance for workflow {WorkflowId}")]
    private static partial void LogRestartedInstance(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Failed to restart instance for workflow {WorkflowId}")]
    private static partial void LogRestartInstanceFailed(ILogger logger, Exception ex, string workflowId);

    private readonly IWorkflowController _controller;
    private readonly ILogger<WorkflowService> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public WorkflowService(
        IWorkflowController controller,
        ILogger<WorkflowService> logger,
        ICorrelationIdService? correlationIdService = null)
    {
        _controller = controller;
        _logger = logger;
        _correlationIdService = correlationIdService ?? new CorrelationIdService();
    }

    public async Task<WorkflowDefinition> ImportWorkflowAsync(string plantUmlText, string? id = null, string? name = null)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = _correlationIdService.GenerateCorrelationId();

        using var scope = _logger.BeginCorrelatedScope(
            _correlationIdService,
            "ImportWorkflow",
            new Dictionary<string, object>
            {
                ["WorkflowId"] = id ?? "auto-generated",
                ["Name"] = name ?? "unnamed"
            });

        try
        {
            _logger.LogOperationStart(_correlationIdService, "ImportWorkflow", new Dictionary<string, object>
            {
                ["WorkflowId"] = id ?? "auto-generated"
            });

            var result = await _controller.ImportWorkflowAsync(plantUmlText, id, name).ConfigureAwait(false);

            sw.Stop();
            _logger.LogOperationComplete(_correlationIdService, "ImportWorkflow", sw.Elapsed, new Dictionary<string, object>
            {
                ["WorkflowId"] = result.Id,
                ["NodeCount"] = result.Nodes.Count,
                ["TransitionCount"] = result.Transitions.Count
            });

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogOperationFailure(_correlationIdService, "ImportWorkflow", ex, sw.Elapsed, new Dictionary<string, object>
            {
                ["WorkflowId"] = id ?? "auto-generated"
            });
            throw;
        }
    }

    public async Task StartInstanceAsync(string workflowId)
    {
        using var scope = _logger.BeginCorrelatedScope(
            _correlationIdService,
            "StartInstance",
            new Dictionary<string, object> { ["WorkflowId"] = workflowId });

        try
        {
            await _controller.StartInstanceAsync(workflowId).ConfigureAwait(false);
            LogStartedInstance(_logger, workflowId);
        }
        catch (Exception ex)
        {
            LogStartInstanceFailed(_logger, ex, workflowId);
            throw;
        }
    }

    public async Task RestartInstanceAsync(string workflowId)
    {
        using var scope = _logger.BeginCorrelatedScope(
            _correlationIdService,
            "RestartInstance",
            new Dictionary<string, object> { ["WorkflowId"] = workflowId });

        try
        {
            await _controller.RestartInstanceAsync(workflowId).ConfigureAwait(false);
            LogRestartedInstance(_logger, workflowId);
        }
        catch (Exception ex)
        {
            LogRestartInstanceFailed(_logger, ex, workflowId);
            throw;
        }
    }

    public async Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId)
    {
        var sw = Stopwatch.StartNew();

        using var scope = _logger.BeginCorrelatedScope(
            _correlationIdService,
            "GetCurrentState",
            new Dictionary<string, object> { ["WorkflowId"] = workflowId });

        try
        {
            var result = await _controller.GetCurrentStatePayloadAsync(workflowId).ConfigureAwait(false);

            sw.Stop();
            _logger.LogOperationComplete(_correlationIdService, "GetCurrentState", sw.Elapsed, new Dictionary<string, object>
            {
                ["WorkflowId"] = workflowId,
                ["IsChoice"] = result.IsChoice,
                ["ChoiceCount"] = result.Choices?.Count ?? 0
            });

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogOperationFailure(_correlationIdService, "GetCurrentState", ex, sw.Elapsed, new Dictionary<string, object>
            {
                ["WorkflowId"] = workflowId
            });
            throw;
        }
    }

    public async Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue)
    {
        var sw = Stopwatch.StartNew();

        using var scope = _logger.BeginCorrelatedScope(
            _correlationIdService,
            "AdvanceByChoice",
            new Dictionary<string, object>
            {
                ["WorkflowId"] = workflowId,
                ["ChoiceValue"] = choiceValue?.ToString() ?? "null"
            });

        try
        {
            _logger.LogOperationStart(_correlationIdService, "AdvanceByChoice", new Dictionary<string, object>
            {
                ["WorkflowId"] = workflowId,
                ["ChoiceValue"] = choiceValue?.ToString() ?? "null"
            });

            var result = await _controller.AdvanceByChoiceValueAsync(workflowId, choiceValue).ConfigureAwait(false);

            sw.Stop();
            _logger.LogOperationComplete(_correlationIdService, "AdvanceByChoice", sw.Elapsed, new Dictionary<string, object>
            {
                ["WorkflowId"] = workflowId,
                ["Success"] = result
            });

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogOperationFailure(_correlationIdService, "AdvanceByChoice", ex, sw.Elapsed, new Dictionary<string, object>
            {
                ["WorkflowId"] = workflowId,
                ["ChoiceValue"] = choiceValue?.ToString() ?? "null"
            });
            throw;
        }
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
