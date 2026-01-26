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

    /// <summary>
    /// Finds workflows by name pattern within a time window.
    /// Used to find location-based workflows by address within 24 hours.
    /// </summary>
    /// <param name="namePattern">Pattern to match workflow name (e.g., "location:address_hash")</param>
    /// <param name="since">Only return workflows created or updated since this time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching workflow entities</returns>
    Task<IEnumerable<WorkflowDefinitionEntity>> FindByNamePatternAsync(
        string namePattern,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}
