using FWH.Common.Workflow.Models;
using DataModels = FWH.Mobile.Data.Models;

namespace FWH.Common.Workflow.Mapping;

/// <summary>
/// Responsible for mapping between domain and data models.
/// Single Responsibility: Convert workflow models between layers.
/// </summary>
public interface IWorkflowModelMapper
{
    /// <summary>
    /// Convert domain model to data model for persistence.
    /// </summary>
    DataModels.WorkflowDefinitionEntity ToDataModel(WorkflowDefinition definition);
}
