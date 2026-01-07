using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FWH.Mobile.Data.Models;

public class WorkflowDefinitionEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<NodeEntity> Nodes { get; set; } = new();

    public List<TransitionEntity> Transitions { get; set; } = new();

    public List<StartPointEntity> StartPoints { get; set; } = new();

    // Persist the current node id for an in-memory instance so instances survive restarts
    public string? CurrentNodeId { get; set; }
}
