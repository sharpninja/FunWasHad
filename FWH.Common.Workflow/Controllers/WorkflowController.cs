using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FWH.Common.Workflow.Models;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Mobile.Data.Repositories;

namespace FWH.Common.Workflow.Controllers;

/// <summary>
/// Controller for workflow business logic operations.
/// Coordinates between workflow service components and handles business rules.
/// Single Responsibility: Orchestrate workflow operations and enforce business logic.
/// </summary>
public class WorkflowController : IWorkflowController
{
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
        _logger.LogDebug("Importing workflow {WorkflowId}", workflowId);

        // Parse PlantUML
        var parser = new PlantUmlParser(plantUmlText);
        var definition = parser.Parse(id, name);

        // Store definition
        _definitionStore.Store(definition);

        // Initialize instance
        await StartInstanceAsync(definition.Id);

        // Persist to database
        await PersistDefinitionAsync(definition);

        _logger.LogInformation("Imported workflow {WorkflowId}", definition.Id);
        return definition;
    }

    public async Task StartInstanceAsync(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        var definition = _definitionStore.Get(workflowId);
        if (definition == null)
            throw new InvalidOperationException($"Unknown workflow id: {workflowId}");

        _logger.LogDebug("Starting instance for workflow {WorkflowId}", workflowId);

        // Try to restore from persistence
        var repo = GetRepository();
        if (repo != null)
        {
            try
            {
                var persisted = await repo.GetByIdAsync(workflowId);
                if (persisted != null && !string.IsNullOrWhiteSpace(persisted.CurrentNodeId))
                {
                    _instanceManager.SetCurrentNode(workflowId, persisted.CurrentNodeId);
                    _logger.LogDebug("Restored workflow {WorkflowId} to node {NodeId}", 
                        workflowId, persisted.CurrentNodeId);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore workflow {WorkflowId} state", workflowId);
            }
        }

        // Calculate and set start node
        var startNode = _stateCalculator.CalculateStartNode(definition);
        _instanceManager.SetCurrentNode(workflowId, startNode);
        
        _logger.LogInformation("Started workflow {WorkflowId} at node {NodeId}", workflowId, startNode);

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

        var definition = _definitionStore.Get(workflowId);
        if (definition == null)
            throw new InvalidOperationException($"Unknown workflow id: {workflowId}");

        _logger.LogDebug("Restarting workflow {WorkflowId}", workflowId);

        // Clear current state
        _instanceManager.ClearCurrentNode(workflowId);

        // Recalculate start node
        await StartInstanceAsync(workflowId);

        var startNode = _instanceManager.GetCurrentNode(workflowId);

        // Persist restart
        var repo = GetRepository();
        if (repo != null)
        {
            try
            {
                await repo.UpdateCurrentNodeIdAsync(workflowId, startNode);
                _logger.LogInformation("Persisted restart for workflow {WorkflowId}", workflowId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist restart for workflow {WorkflowId}", workflowId);
            }
        }
    }

    public Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        var definition = _definitionStore.Get(workflowId);
        if (definition == null)
            throw new InvalidOperationException($"Unknown workflow id: {workflowId}");

        var currentNodeId = _instanceManager.GetCurrentNode(workflowId);
        var payload = _stateCalculator.CalculateCurrentPayload(definition, currentNodeId);

        return Task.FromResult(payload);
    }

    public async Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentNullException(nameof(workflowId));

        var definition = _definitionStore.Get(workflowId);
        if (definition == null)
            throw new InvalidOperationException($"Unknown workflow id: {workflowId}");

        var currentNodeId = _instanceManager.GetCurrentNode(workflowId);
        if (currentNodeId == null)
        {
            await StartInstanceAsync(workflowId);
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
        await PersistCurrentNodeAsync(workflowId, newNodeId);

        _logger.LogInformation("Advanced workflow {WorkflowId} to node {NodeId}", workflowId, newNodeId);

        // After advancing, if the new node is an action node execute it and auto-advance if possible
        var newNodeObj = definition.Nodes.FirstOrDefault(n => n.Id == newNodeId);
        if (newNodeObj != null)
        {
            await TryExecuteNodeActionAsync(definition, workflowId, newNodeObj);
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
            
            await repo.UpdateCurrentNodeIdAsync(workflowId, nodeId);
            _logger.LogDebug("Persisted node {NodeId} for workflow {WorkflowId}", nodeId, workflowId);
        }
        catch (Exception ex)
        {
            using var warnScope = _logger.BeginScope(new Dictionary<string, object> 
            { 
                ["Operation"] = "UpdateCurrentNodeId", 
                ["WorkflowId"] = workflowId,
                ["NodeId"] = nodeId ?? "null"
            });
            
            _logger.LogWarning(ex, "Failed to persist node for workflow {WorkflowId}", workflowId);
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
            
            await repo.CreateAsync(dataModel);
            _logger.LogInformation("Persisted workflow {WorkflowId}", definition.Id);
        }
        catch (Exception ex)
        {
            using var errScope = _logger.BeginScope(new Dictionary<string, object> 
            { 
                ["Operation"] = "PersistDefinition", 
                ["WorkflowId"] = definition.Id 
            });
            
            _logger.LogError(ex, "Failed to persist workflow {WorkflowId}", definition.Id);
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
            var executed = await _actionExecutor.ExecuteAsync(workflowId, node, definition);
            if (!executed) return;

            // If executed and there is a single outgoing transition, advance automatically
            var outgoing = definition.Transitions.Where(t => t.FromNodeId == node.Id).ToList();
            if (outgoing.Count == 1)
            {
                var next = outgoing[0].ToNodeId;
                _instanceManager.SetCurrentNode(workflowId, next);
                await PersistCurrentNodeAsync(workflowId, next);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error executing action for node {NodeId}", node.Id);
        }
    }
}
