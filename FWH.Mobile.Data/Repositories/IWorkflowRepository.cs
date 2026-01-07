using FWH.Mobile.Data.Models;

namespace FWH.Mobile.Data.Repositories;

public interface IWorkflowRepository
{
    Task<WorkflowDefinitionEntity> CreateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default);
    Task<WorkflowDefinitionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinitionEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WorkflowDefinitionEntity> UpdateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> UpdateCurrentNodeIdAsync(string workflowDefinitionId, string? currentNodeId, CancellationToken cancellationToken = default);
}
