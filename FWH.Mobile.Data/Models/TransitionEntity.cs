using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FWH.Mobile.Data.Models;

public class TransitionEntity
{
    [Key]
    public long Id { get; set; }

    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;
    public string? Condition { get; set; }

    public string? WorkflowDefinitionEntityId { get; set; }

    [ForeignKey(nameof(WorkflowDefinitionEntityId))]
    public WorkflowDefinitionEntity? WorkflowDefinition { get; set; }
}
