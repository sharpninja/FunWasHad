namespace FWH.Common.Workflow.Models;

// Added JsonMetadata as a new property to hold structured JSON attached to a node (left-side of note).
public record WorkflowNode(string Id, string Label, string? JsonMetadata = null, string? NoteMarkdown = null);
