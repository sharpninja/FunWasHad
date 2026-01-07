using Microsoft.EntityFrameworkCore;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Models;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Data.Repositories;

public class EfWorkflowRepository : IWorkflowRepository
{
    private readonly NotesDbContext _context;
    private readonly ILogger<EfWorkflowRepository> _logger;

    public EfWorkflowRepository(NotesDbContext context, ILogger<EfWorkflowRepository>? logger = null)
    {
        _context = context;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EfWorkflowRepository>.Instance;
    }

    public async Task<WorkflowDefinitionEntity> CreateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "CreateWorkflow", ["WorkflowId"] = def.Id });
        try
        {
            _logger.LogDebug("Creating workflow {WorkflowId}", def.Id);
            var entry = await _context.WorkflowDefinitions.AddAsync(def, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created workflow {WorkflowId}", def.Id);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow {WorkflowId}", def?.Id);
            throw;
        }
    }

    public async Task<WorkflowDefinitionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "GetById", ["WorkflowId"] = id });
        try
        {
            _logger.LogDebug("Loading workflow {WorkflowId}", id);
            return await _context.WorkflowDefinitions
                .Include(w => w.Nodes)
                .Include(w => w.Transitions)
                .Include(w => w.StartPoints)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading workflow {WorkflowId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<WorkflowDefinitionEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "GetAll" });
        try
        {
            _logger.LogDebug("Loading all workflows");
            return await _context.WorkflowDefinitions
                .Include(w => w.Nodes)
                .Include(w => w.Transitions)
                .Include(w => w.StartPoints)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading all workflows");
            throw;
        }
    }

    public async Task<WorkflowDefinitionEntity> UpdateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "UpdateWorkflow", ["WorkflowId"] = def.Id });
        try
        {
            _logger.LogDebug("Updating workflow {WorkflowId}", def.Id);
            var existing = await _context.WorkflowDefinitions
                .Include(w => w.Nodes)
                .Include(w => w.Transitions)
                .Include(w => w.StartPoints)
                .FirstOrDefaultAsync(w => w.Id == def.Id, cancellationToken);

            if (existing == null)
            {
                _logger.LogWarning("Workflow {WorkflowId} not found for update", def.Id);
                throw new KeyNotFoundException($"Workflow '{def.Id}' not found");
            }

            existing.Name = def.Name;
            existing.CurrentNodeId = def.CurrentNodeId;

            // Remove existing children by querying the DbSets to ensure they have database-generated keys
            var oldNodes = await _context.NodeEntities.Where(n => n.WorkflowDefinitionEntityId == existing.Id).ToListAsync(cancellationToken);
            if (oldNodes.Any()) _context.NodeEntities.RemoveRange(oldNodes);

            var oldTransitions = await _context.TransitionEntities.Where(t => t.WorkflowDefinitionEntityId == existing.Id).ToListAsync(cancellationToken);
            if (oldTransitions.Any()) _context.TransitionEntities.RemoveRange(oldTransitions);

            var oldStartPoints = await _context.StartPointEntities.Where(s => s.WorkflowDefinitionEntityId == existing.Id).ToListAsync(cancellationToken);
            if (oldStartPoints.Any()) _context.StartPointEntities.RemoveRange(oldStartPoints);

            // Clear tracked navigation collections
            existing.Nodes.Clear();
            existing.Transitions.Clear();
            existing.StartPoints.Clear();

            // Persist removals first so we can add fresh children cleanly
            await _context.SaveChangesAsync(cancellationToken);

            // Prepare new children, creating fresh instances so tracking is clean
            var newNodes = def.Nodes.Select(n => new NodeEntity
            {
                NodeId = n.NodeId,
                Text = n.Text,
                Type = n.Type,
                WorkflowDefinitionEntityId = existing.Id
            }).ToList();

            var newTransitions = def.Transitions.Select(t => new TransitionEntity
            {
                FromNodeId = t.FromNodeId,
                ToNodeId = t.ToNodeId,
                Condition = t.Condition,
                WorkflowDefinitionEntityId = existing.Id
            }).ToList();

            var newStartPoints = def.StartPoints.Select(s => new StartPointEntity
            {
                NodeId = s.NodeId,
                WorkflowDefinitionEntityId = existing.Id
            }).ToList();

            if (newNodes.Any()) await _context.NodeEntities.AddRangeAsync(newNodes, cancellationToken);
            if (newTransitions.Any()) await _context.TransitionEntities.AddRangeAsync(newTransitions, cancellationToken);
            if (newStartPoints.Any()) await _context.StartPointEntities.AddRangeAsync(newStartPoints, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            // Reload and return the updated workflow with navigation properties
            var updated = await GetByIdAsync(existing.Id, cancellationToken);
            _logger.LogInformation("Updated workflow {WorkflowId}", def.Id);
            return updated!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId}", def?.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "DeleteWorkflow", ["WorkflowId"] = id });
        try
        {
            _logger.LogDebug("Deleting workflow {WorkflowId}", id);
            var entity = await _context.WorkflowDefinitions.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) return false;
            _context.WorkflowDefinitions.Remove(entity);
            var affected = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted workflow {WorkflowId}", id);
            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", id);
            throw;
        }
    }

    public async Task<bool> UpdateCurrentNodeIdAsync(string workflowDefinitionId, string? currentNodeId = null, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "UpdateCurrentNodeId", ["WorkflowId"] = workflowDefinitionId, ["NodeId"] = currentNodeId ?? "null" });
        try
        {
            _logger.LogDebug("Updating CurrentNodeId for {WorkflowId} to {NodeId}", workflowDefinitionId, currentNodeId);
            var existing = await _context.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == workflowDefinitionId, cancellationToken);
            if (existing == null)
            {
                _logger.LogWarning("Workflow {WorkflowId} not found when updating CurrentNodeId", workflowDefinitionId);
                return false;
            }
            existing.CurrentNodeId = currentNodeId;
            _context.WorkflowDefinitions.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated CurrentNodeId for {WorkflowId} to {NodeId}", workflowDefinitionId, currentNodeId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CurrentNodeId for {WorkflowId} to {NodeId}", workflowDefinitionId, currentNodeId);
            throw;
        }
    }
}
