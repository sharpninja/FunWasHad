using FWH.Common.Workflow.Models;
using DataModels = FWH.Mobile.Data.Models;

namespace FWH.Common.Workflow.Mapping;

/// <summary>
/// Maps workflow models between domain and data layers.
/// Single Responsibility: Model conversion logic.
/// </summary>
public class WorkflowModelMapper : IWorkflowModelMapper
{
    public DataModels.WorkflowDefinitionEntity ToDataModel(WorkflowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var data = new DataModels.WorkflowDefinitionEntity
        {
            Id = string.IsNullOrWhiteSpace(definition.Id) ? Guid.NewGuid().ToString() : definition.Id,
            Name = definition.Name ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var n in definition.Nodes)
        {
            data.Nodes.Add(new DataModels.NodeEntity
            {
                NodeId = n.Id,
                Text = n.Label
            });
        }

        foreach (var t in definition.Transitions)
        {
            data.Transitions.Add(new DataModels.TransitionEntity
            {
                FromNodeId = t.FromNodeId,
                ToNodeId = t.ToNodeId,
                Condition = t.Condition
            });
        }

        foreach (var s in definition.StartPoints)
        {
            data.StartPoints.Add(new DataModels.StartPointEntity
            {
                NodeId = s.NodeId
            });
        }

        // Set initial current node as the start point if available
        data.CurrentNodeId = definition.StartPoints.FirstOrDefault()?.NodeId ?? definition.Nodes.FirstOrDefault()?.Id;

        return data;
    }
}
