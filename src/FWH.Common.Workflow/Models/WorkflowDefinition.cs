using System.Collections.Generic;

namespace FWH.Common.Workflow.Models;

public record WorkflowDefinition(
    string Id,
    string Name,
    IReadOnlyList<WorkflowNode> Nodes,
    IReadOnlyList<Transition> Transitions,
    IReadOnlyList<StartPoint> StartPoints);
