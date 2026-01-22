namespace FWH.Common.Workflow.Models;

public record Transition(string Id, string FromNodeId, string ToNodeId, string? Condition = null);
