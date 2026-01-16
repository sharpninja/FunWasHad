using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using FWH.Common.Workflow.Models;
using System.Text.Json;

namespace FWH.Common.Workflow.State;

/// <summary>
/// Calculates workflow state and transitions.
/// Single Responsibility: State calculation logic.
/// </summary>
public class WorkflowStateCalculator : IWorkflowStateCalculator
{
    private readonly ILogger<WorkflowStateCalculator> _logger;

    public WorkflowStateCalculator(ILogger<WorkflowStateCalculator> logger)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkflowStateCalculator>.Instance;
    }

    public string? CalculateStartNode(WorkflowDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));

        var start = definition.StartPoints.FirstOrDefault()?.NodeId ?? definition.Nodes.FirstOrDefault()?.Id;

        if (!string.IsNullOrWhiteSpace(start))
        {
            var outgoing = definition.Transitions.Where(t => t.FromNodeId == start).ToList();

            // Only auto-advance if the start node is literally named "start" (case-insensitive)
            // or if it's an implicit start point without a label
            var startNode = definition.Nodes.FirstOrDefault(n => n.Id == start);
            var shouldAutoAdvance = startNode == null || 
                                   string.Equals(startNode.Label, "start", StringComparison.OrdinalIgnoreCase) ||
                                   string.IsNullOrWhiteSpace(startNode.Label);

            if (shouldAutoAdvance && outgoing.Count == 1)
            {
                var targetId = outgoing[0].ToNodeId;
                var targetNode = definition.Nodes.FirstOrDefault(n => n.Id == targetId);

                if (targetNode != null)
                {
                    if (targetNode.Label?.StartsWith("if:", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogDebug("Advanced to decision node {NodeId}", targetId);
                        return targetId;
                    }

                    _logger.LogDebug("Advanced to first action node {NodeId}", targetId);
                    return targetId;
                }
            }
        }

        return start;
    }

    public WorkflowStatePayload CalculateCurrentPayload(WorkflowDefinition definition, string? currentNodeId)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));

        var node = definition.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
        var outgoing = definition.Transitions.Where(t => t.FromNodeId == currentNodeId).ToList();

        var isChoice = outgoing.Count > 1 || outgoing.Any(t => !string.IsNullOrWhiteSpace(t.Condition));

        if (isChoice)
        {
            var choices = outgoing.Select((t, idx) =>
            {
                var target = definition.Nodes.FirstOrDefault(n => n.Id == t.ToNodeId);
                var display = target?.Label ?? t.ToNodeId;
                return new WorkflowChoiceOption(idx, display, t.ToNodeId, t.Condition);
            }).ToList();

            return new WorkflowStatePayload(true, node?.NoteMarkdown, choices, node?.Label);
        }

        // If the node contains a JSON action in the note, detect and present a concise action hint
        string? text = null;
        if (!string.IsNullOrWhiteSpace(node?.NoteMarkdown))
        {
            var note = node!.NoteMarkdown!.Trim();
            if (note.StartsWith('{') && note.EndsWith('}'))
            {
                try
                {
                    using var doc = JsonDocument.Parse(note);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("action", out var actionProp))
                    {
                        var actionName = actionProp.GetString();
                        text = actionName != null ? $"Action: {actionName}" : node.NoteMarkdown;
                    }
                    else
                    {
                        text = node.NoteMarkdown;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(ex, "Failed to parse JSON action in node {NodeId}", node.Id);
                    text = node.NoteMarkdown;
                }
            }
        }

        text ??= !string.IsNullOrWhiteSpace(node?.NoteMarkdown) ? node!.NoteMarkdown : node?.Label;
        return new WorkflowStatePayload(false, text, Array.Empty<WorkflowChoiceOption>(), node?.Label);
    }
}
