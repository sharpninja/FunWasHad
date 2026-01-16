using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Common.Workflow.Models;

public class WorkflowModel
{
}

public record WorkflowDefinition(
    string Id,
    string Name,
    IReadOnlyList<WorkflowNode> Nodes,
    IReadOnlyList<Transition> Transitions,
    IReadOnlyList<StartPoint> StartPoints);

// Added JsonMetadata as a new property to hold structured JSON attached to a node (left-side of note).
public record WorkflowNode(string Id, string Label, string? JsonMetadata = null, string? NoteMarkdown = null);

public record Transition(string Id, string FromNodeId, string ToNodeId, string? Condition = null);

public record StartPoint(string NodeId);

public partial class WorkflowStateModel : ObservableObject
{
    private ConcurrentDictionary<string, WorkflowModel> Workflows = new();

    public bool TryGetWorkflow(string id, out WorkflowModel? model) => Workflows.TryGetValue(id, out model);

    public void AddOrUpdateWorkflow(string id, WorkflowModel model) => Workflows[id] = model;
}
