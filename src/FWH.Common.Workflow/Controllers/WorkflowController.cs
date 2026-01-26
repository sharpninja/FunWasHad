using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.Models;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Storage;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Workflow.Controllers;

/// <summary>
/// Controller for workflow business logic operations.
/// Coordinates between workflow service components and handles business rules.
/// Single Responsibility: Orchestrate workflow operations and enforce business logic.
/// </summary>
public partial class WorkflowController : IWorkflowController
{
    [LoggerMessage(LogLevel.Debug, "Importing workflow {WorkflowId}")]
    private static partial void LogImportingWorkflow(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Workflow {WorkflowId} already exists in definition store, reusing existing definition")]
    private static partial void LogWorkflowExists(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Parsing PlantUML for workflow {WorkflowId} (content length: {Length} chars)")]
    private static partial void LogParsingPlantUml(ILogger logger, string workflowId, int length);

    [LoggerMessage(LogLevel.Debug, "Completed parsing workflow {WorkflowId} - {NodeCount} nodes, {TransitionCount} transitions")]
    private static partial void LogParsingCompleted(ILogger logger, string workflowId, int nodeCount, int transitionCount);

    [LoggerMessage(LogLevel.Information, "Imported workflow {WorkflowId}")]
    private static partial void LogImportedWorkflow(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Starting instance for workflow {WorkflowId}")]
    private static partial void LogStartingInstance(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Restored workflow {WorkflowId} to node {NodeId}")]
    private static partial void LogRestoredWorkflow(ILogger logger, string workflowId, string nodeId);

    [LoggerMessage(LogLevel.Warning, "Failed to restore workflow {WorkflowId} state")]
    private static partial void LogRestoreFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Information, "Started workflow {WorkflowId} at node {NodeId}")]
    private static partial void LogStartedWorkflow(ILogger logger, string workflowId, string nodeId);

    [LoggerMessage(LogLevel.Debug, "Restarting workflow {WorkflowId}")]
    private static partial void LogRestartingWorkflow(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Restarted workflow {WorkflowId} at node {NodeId}")]
    private static partial void LogRestartedWorkflow(ILogger logger, string workflowId, string nodeId);

    [LoggerMessage(LogLevel.Information, "Persisted restart for workflow {WorkflowId}")]
    private static partial void LogPersistedRestart(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Warning, "Failed to persist restart for workflow {WorkflowId}")]
    private static partial void LogPersistRestartFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Information, "Advanced workflow {WorkflowId} to node {NodeId}")]
    private static partial void LogAdvancedWorkflow(ILogger logger, string workflowId, string nodeId);

    [LoggerMessage(LogLevel.Debug, "Persisted node {NodeId} for workflow {WorkflowId}")]
    private static partial void LogPersistedNode(ILogger logger, string? nodeId, string workflowId);

    [LoggerMessage(LogLevel.Warning, "Failed to persist node for workflow {WorkflowId}")]
    private static partial void LogPersistNodeFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Workflow {WorkflowId} already exists, updating instead of creating")]
    private static partial void LogWorkflowExistsUpdating(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Updated existing workflow {WorkflowId}")]
    private static partial void LogUpdatedWorkflow(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Created new workflow {WorkflowId}")]
    private static partial void LogCreatedWorkflow(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Failed to persist workflow {WorkflowId}")]
    private static partial void LogPersistWorkflowFailed(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Warning, "Error executing action for node {NodeId}")]
    private static partial void LogActionExecutionError(ILogger logger, Exception ex, string nodeId);

    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowDefinitionStore _definitionStore;
    private readonly IWorkflowInstanceManager _instanceManager;
    private readonly IWorkflowModelMapper _mapper;
    private readonly IWorkflowStateCalculator _stateCalculator;
    private readonly ILogger<WorkflowController> _logger;
    private readonly FWH.Common.Workflow.Actions.IWorkflowActionExecutor _actionExecutor;

    public WorkflowController(
        IServiceProvider serviceProvider,
        IWorkflowDefinitionStore definitionStore,
        IWorkflowInstanceManager instanceManager,
        IWorkflowModelMapper mapper,
        IWorkflowStateCalculator stateCalculator,
        FWH.Common.Workflow.Actions.IWorkflowActionExecutor actionExecutor,
        ILogger<WorkflowController>? logger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _definitionStore = definitionStore ?? throw new ArgumentNullException(nameof(definitionStore));
        _instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _stateCalculator = stateCalculator ?? throw new ArgumentNullException(nameof(stateCalculator));
        _actionExecutor = actionExecutor ?? throw new ArgumentNullException(nameof(actionExecutor));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowController>.Instance;
    }

    public async Task<WorkflowDefinition> ImportWorkflowAsync(string plantUmlText, string? id = null, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(plantUmlText))
            throw new ArgumentNullException(nameof(plantUmlText));

        var workflowId = id ?? Guid.NewGuid().ToString();
        LogImportingWorkflow(_logger, workflowId);

        // Check if workflow definition already exists in memory store
        var existingDefinition = _definitionStore.GetById(workflowId);
        if (existingDefinition != null)
        {
            LogWorkflowExists(_logger, workflowId);

            // Ensure instance is started
            await StartInstanceAsync(workflowId).ConfigureAwait(false);
            return existingDefinition;
        }

        // Parse PlantUML only if not already in store
        LogParsingPlantUml(_logger, workflowId, plantUmlText.Length);
        var parser = new PlantUmlParser(plantUmlText);
        var definition = parser.Parse(id, name);
        LogParsingCompleted(_logger, workflowId, definition.Nodes.Count, definition.Transitions.Count);

        // Store definition
        _definitionStore.Store(definition);

        // Initialize instance
        await StartInstanceAsync(definition.Id).ConfigureAwait(false);

        // Persist to database
        await PersistDefinitionAsync(definition).ConfigureAwait(false);

        LogImportedWorkflow(_logger, definition.Id);
        return definition;
    }

    public async Task StartInstanceAsync(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        var definition = _definitionStore.GetById(workflowId) ?? throw new InvalidOperationException($"Unknown workflow id: {workflowId}");
        LogStartingInstance(_logger, workflowId);

        // Try to restore from persistence
        var repo = GetRepository();
        if (repo != null)
        {
            try
            {
                var persisted = await repo.GetByIdAsync(workflowId).ConfigureAwait(false);
                if (persisted != null && !string.IsNullOrWhiteSpace(persisted.CurrentNodeId))
                {
                    _instanceManager.SetCurrentNode(workflowId, persisted.CurrentNodeId);
                    LogRestoredWorkflow(_logger, workflowId, persisted.CurrentNodeId);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogRestoreFailed(_logger, ex, workflowId);
            }
        }

        // Calculate and set start node
        var startNode = _stateCalculator.CalculateStartNode(definition);
        _instanceManager.SetCurrentNode(workflowId, startNode);

        LogStartedWorkflow(_logger, workflowId, startNode);

        // Also attempt to execute an inline action attached to the original start node (before auto-advance)
        var originalStart = definition.StartPoints.FirstOrDefault()?.NodeId ?? definition.Nodes.FirstOrDefault()?.Id;
        if (!string.IsNullOrWhiteSpace(originalStart))
        {
            var originalNode = definition.Nodes.FirstOrDefault(n => n.Id == originalStart);
            if (originalNode != null && !string.IsNullOrWhiteSpace(originalNode.NoteMarkdown))
            {
                var note = originalNode.NoteMarkdown.Trim();
                if (note.StartsWith('{') && note.EndsWith('}'))
                {
                    _ = TryExecuteNodeActionAsync(definition, workflowId, originalNode);
                }
            }
        }

        // If the start node (as selected) is an action node attempt to execute it
        if (!string.IsNullOrWhiteSpace(startNode))
        {
            var startNodeObj = definition.Nodes.FirstOrDefault(n => n.Id == startNode);
            if (startNodeObj != null)
            {
                _ = TryExecuteNodeActionAsync(definition, workflowId, startNodeObj);
            }
        }
    }

    public async Task RestartInstanceAsync(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        var definition = _definitionStore.GetById(workflowId) ?? throw new InvalidOperationException($"Unknown workflow id: {workflowId}");
        LogRestartingWorkflow(_logger, workflowId);

        // Clear current state
        _instanceManager.ClearCurrentNode(workflowId);

        // Calculate new start node (don't call StartInstanceAsync as it would try to restore from DB)
        var startNode = _stateCalculator.CalculateStartNode(definition);
        _instanceManager.SetCurrentNode(workflowId, startNode);

        LogRestartedWorkflow(_logger, workflowId, startNode);

        // Persist the restart (update DB to new start node)
        var repo = GetRepository();
        if (repo != null)
        {
            try
            {
                await repo.UpdateCurrentNodeIdAsync(workflowId, startNode).ConfigureAwait(false);
                LogPersistedRestart(_logger, workflowId);
            }
            catch (Exception ex)
            {
                LogPersistRestartFailed(_logger, ex, workflowId);
            }
        }

        // Execute action on start node if present
        if (!string.IsNullOrWhiteSpace(startNode))
        {
            var startNodeObj = definition.Nodes.FirstOrDefault(n => n.Id == startNode);
            if (startNodeObj != null)
            {
                await TryExecuteNodeActionAsync(definition, workflowId, startNodeObj).ConfigureAwait(false);
            }
        }
    }

    public Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        var definition = _definitionStore.GetById(workflowId) ?? throw new InvalidOperationException($"Unknown workflow id: {workflowId}");
        var currentNodeId = _instanceManager.GetCurrentNode(workflowId);
        var payload = _stateCalculator.CalculateCurrentPayload(definition, currentNodeId);

        return Task.FromResult(payload);
    }

    public async Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        var definition = _definitionStore.GetById(workflowId) ?? throw new InvalidOperationException($"Unknown workflow id: {workflowId}");
        var currentNodeId = _instanceManager.GetCurrentNode(workflowId);
        if (currentNodeId == null)
        {
            await StartInstanceAsync(workflowId).ConfigureAwait(false);
            currentNodeId = _instanceManager.GetCurrentNode(workflowId);
        }

        // Find matching transition
        var outgoing = definition.Transitions.Where(t => t.FromNodeId == currentNodeId).ToList();
        if (outgoing.Count == 0)
            return false;

        string? newNodeId = ResolveChoiceValue(outgoing, definition, choiceValue);
        if (newNodeId == null)
            return false;

        // Update state
        _instanceManager.SetCurrentNode(workflowId, newNodeId);

        // Persist state
        await PersistCurrentNodeAsync(workflowId, newNodeId).ConfigureAwait(false);

        LogAdvancedWorkflow(_logger, workflowId, newNodeId);

        // After advancing, if the new node is an action node execute it and auto-advance if possible
        var newNodeObj = definition.Nodes.FirstOrDefault(n => n.Id == newNodeId);
        if (newNodeObj != null)
        {
            await TryExecuteNodeActionAsync(definition, workflowId, newNodeObj).ConfigureAwait(false);
        }

        return true;
    }

    public string? GetCurrentNodeId(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            return null;

        return _instanceManager.GetCurrentNode(workflowId);
    }

    public bool WorkflowExists(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            return false;

        return _definitionStore.Exists(workflowId);
    }

    private string? ResolveChoiceValue(
        List<Transition> outgoing,
        WorkflowDefinition definition,
        object? choiceValue)
    {
        // Match by node ID
        if (choiceValue is string sVal)
        {
            var match = outgoing.FirstOrDefault(t =>
                string.Equals(t.ToNodeId, sVal, StringComparison.Ordinal));
            if (match != null)
                return match.ToNodeId;

            // Match by node label
            var byLabel = outgoing.FirstOrDefault(t =>
                definition.Nodes.FirstOrDefault(n => n.Id == t.ToNodeId)?.Label == sVal);
            if (byLabel != null)
                return byLabel.ToNodeId;
        }

        // Match by index
        if (choiceValue is int idx && idx >= 0 && idx < outgoing.Count)
            return outgoing[idx].ToNodeId;

        // Match by numeric string
        if (choiceValue is string numeric && int.TryParse(numeric, out var parsed))
        {
            if (parsed >= 0 && parsed < outgoing.Count)
                return outgoing[parsed].ToNodeId;
        }

        // Auto-advance on single transition
        if (choiceValue == null && outgoing.Count == 1)
            return outgoing[0].ToNodeId;

        return null;
    }

    private async Task PersistCurrentNodeAsync(string workflowId, string? nodeId)
    {
        var repo = GetRepository();
        if (repo == null)
            return;

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = "UpdateCurrentNodeId",
                ["WorkflowId"] = workflowId,
                ["NodeId"] = nodeId ?? "null"
            });

            await repo.UpdateCurrentNodeIdAsync(workflowId, nodeId).ConfigureAwait(false);
            LogPersistedNode(_logger, nodeId, workflowId);
        }
        catch (Exception ex)
        {
            using var warnScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = "UpdateCurrentNodeId",
                ["WorkflowId"] = workflowId,
                ["NodeId"] = nodeId ?? "null"
            });

            LogPersistNodeFailed(_logger, ex, workflowId);
        }
    }

    private async Task PersistDefinitionAsync(WorkflowDefinition definition)
    {
        var repo = GetRepository();
        if (repo == null)
            return;

        try
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = "PersistDefinition",
                ["WorkflowId"] = definition.Id
            });

            var dataModel = _mapper.ToDataModel(definition);
            dataModel.CurrentNodeId = _instanceManager.GetCurrentNode(definition.Id) ?? dataModel.CurrentNodeId;

            // Check if workflow already exists - use upsert pattern
            var existing = await repo.GetByIdAsync(definition.Id).ConfigureAwait(false);
            if (existing != null)
            {
                LogWorkflowExistsUpdating(_logger, definition.Id);
                await repo.UpdateAsync(dataModel).ConfigureAwait(false);
                LogUpdatedWorkflow(_logger, definition.Id);
            }
            else
            {
                await repo.CreateAsync(dataModel).ConfigureAwait(false);
                LogCreatedWorkflow(_logger, definition.Id);
            }
        }
        catch (Exception ex)
        {
            using var errScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = "PersistDefinition",
                ["WorkflowId"] = definition.Id
            });

            LogPersistWorkflowFailed(_logger, ex, definition.Id);
        }
    }

    private IWorkflowRepository? GetRepository()
    {
        return _serviceProvider.GetService<IWorkflowRepository>();
    }

    private async Task TryExecuteNodeActionAsync(WorkflowDefinition definition, string workflowId, WorkflowNode node)
    {
        try
        {
            var executed = await _actionExecutor.ExecuteAsync(workflowId, node, definition).ConfigureAwait(false);
            if (!executed) return;

            // If executed and there is a single outgoing transition, advance automatically
            var outgoing = definition.Transitions.Where(t => t.FromNodeId == node.Id).ToList();
            if (outgoing.Count == 1)
            {
                var next = outgoing[0].ToNodeId;
                _instanceManager.SetCurrentNode(workflowId, next);
                await PersistCurrentNodeAsync(workflowId, next).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogActionExecutionError(_logger, ex, node.Id);
        }
    }
}
