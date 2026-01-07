using System;
using System.Linq;
using FWH.Common.Workflow.Models;
using DataModels = FWH.Mobile.Data.Models;

namespace FWH.Common.Workflow.Mapping;

/// <summary>
/// Maps workflow models between domain and data layers.
/// Single Responsibility: Model conversion logic.
/// </summary>
public class WorkflowModelMapper : IWorkflowModelMapper
{
    public DataModels.WorkflowDefinitionEntity ToDataModel(WorkflowDefinition def)
    {
        if (def == null) throw new ArgumentNullException(nameof(def));

        var data = new DataModels.WorkflowDefinitionEntity
        {
            Id = string.IsNullOrWhiteSpace(def.Id) ? Guid.NewGuid().ToString() : def.Id,
            Name = def.Name ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var n in def.Nodes)
        {
            data.Nodes.Add(new DataModels.NodeEntity
            {
                NodeId = n.Id,
                Text = n.Label
            });
        }

        foreach (var t in def.Transitions)
        {
            data.Transitions.Add(new DataModels.TransitionEntity
            {
                FromNodeId = t.FromNodeId,
                ToNodeId = t.ToNodeId,
                Condition = t.Condition
            });
        }

        foreach (var s in def.StartPoints)
        {
            data.StartPoints.Add(new DataModels.StartPointEntity
            {
                NodeId = s.NodeId
            });
        }

        // Set initial current node as the start point if available
        data.CurrentNodeId = def.StartPoints.FirstOrDefault()?.NodeId ?? def.Nodes.FirstOrDefault()?.Id;

        return data;
    }
}
