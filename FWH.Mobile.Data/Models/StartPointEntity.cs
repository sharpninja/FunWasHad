using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FWH.Mobile.Data.Models;

public class StartPointEntity
{
    [Key]
    public long Id { get; set; }

    public string? NodeId { get; set; }

    public string? WorkflowDefinitionEntityId { get; set; }

    [ForeignKey(nameof(WorkflowDefinitionEntityId))]
    public WorkflowDefinitionEntity? WorkflowDefinition { get; set; }
}
