using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FWH.Mobile.Data.Models;

public class NodeEntity
{
    [Key]
    public long Id { get; set; }

    public string NodeId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string? Type { get; set; }

    public string? WorkflowDefinitionEntityId { get; set; }

    [ForeignKey(nameof(WorkflowDefinitionEntityId))]
    public WorkflowDefinitionEntity? WorkflowDefinition { get; set; }
}
