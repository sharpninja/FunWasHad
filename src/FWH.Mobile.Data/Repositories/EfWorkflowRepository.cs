using System.Diagnostics.CodeAnalysis;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Data.Repositories;

public partial class EfWorkflowRepository : IWorkflowRepository
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
        ArgumentNullException.ThrowIfNull(def);
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "CreateWorkflow", ["WorkflowId"] = def.Id });
        try
        {
            LogCreating(_logger, def.Id);

            // Check if workflow already exists before attempting to create
            var existing = await _context.WorkflowDefinitions
                .FirstOrDefaultAsync(w => w.Id == def.Id, cancellationToken).ConfigureAwait(false);

            if (existing != null)
            {
                LogAlreadyExists(_logger, def.Id);
                throw new InvalidOperationException($"Workflow '{def.Id}' already exists. Use UpdateAsync to modify it.");
            }

            var entry = await _context.WorkflowDefinitions.AddAsync(def, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogCreated(_logger, def.Id);
            return entry.Entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UNIQUE constraint", StringComparison.Ordinal) == true)
        {
            LogUniqueConstraintCreate(_logger, ex, def?.Id);
            throw new InvalidOperationException($"Workflow '{def?.Id}' already exists. Use UpdateAsync to modify it.", ex);
        }
        catch (Exception ex)
        {
            LogCreateError(_logger, ex, def?.Id);
            throw;
        }
    }

    public async Task<WorkflowDefinitionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "GetById", ["WorkflowId"] = id });
        try
        {
            LogLoading(_logger, id);
            return await _context.WorkflowDefinitions
                .Include(w => w.Nodes)
                .Include(w => w.Transitions)
                .Include(w => w.StartPoints)
                .AsSplitQuery()
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogLoadingCancelled(_logger, id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            LogLoadDbError(_logger, ex, id);
            throw;
        }
        catch (Exception ex)
        {
            LogLoadError(_logger, ex, id);
            throw;
        }
    }

    public async Task<IEnumerable<WorkflowDefinitionEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "GetAll" });
        try
        {
            LogLoadingAll(_logger);
            return await _context.WorkflowDefinitions
                .Include(w => w.Nodes)
                .Include(w => w.Transitions)
                .Include(w => w.StartPoints)
                .AsSplitQuery()
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogLoadingAllCancelled(_logger);
            throw;
        }
        catch (DbUpdateException ex)
        {
            LogLoadAllDbError(_logger, ex);
            throw;
        }
        catch (Exception ex)
        {
            LogLoadAllError(_logger, ex);
            throw;
        }
    }

    [SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "EF Core translates Contains to SQL LIKE; StringComparison is not supported in database translation.")]
    public async Task<IEnumerable<WorkflowDefinitionEntity>> FindByNamePatternAsync(
        string namePattern,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "FindByNamePattern",
            ["Pattern"] = namePattern,
            ["Since"] = since
        });

        try
        {
            LogFindByPattern(_logger, namePattern, since);

            // Convert DateTimeOffset to DateTime for SQLite compatibility
            var sinceDateTime = since.UtcDateTime;

            return await _context.WorkflowDefinitions
                .Include(w => w.Nodes)
                .Include(w => w.Transitions)
                .Include(w => w.StartPoints)
                .AsSplitQuery()
                .Where(w => w.Name.Contains(namePattern) && w.CreatedAt >= sinceDateTime)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogFindByPatternCancelled(_logger, namePattern);
            throw;
        }
        catch (DbUpdateException ex)
        {
            LogFindByPatternDbError(_logger, ex, namePattern);
            throw;
        }
        catch (Exception ex)
        {
            LogFindByPatternError(_logger, ex, namePattern);
            throw;
        }
    }

    public async Task<WorkflowDefinitionEntity> UpdateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(def);
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "UpdateWorkflow", ["WorkflowId"] = def.Id });

        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                LogUpdating(_logger, def.Id, retryCount + 1);
                var existing = await _context.WorkflowDefinitions
                    .Include(w => w.Nodes)
                    .Include(w => w.Transitions)
                    .Include(w => w.StartPoints)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(w => w.Id == def.Id, cancellationToken).ConfigureAwait(false);

                if (existing == null)
                {
                    LogUpdateNotFound(_logger, def.Id);
                    throw new KeyNotFoundException($"Workflow '{def.Id}' not found");
                }

                existing.Name = def.Name;
                existing.CurrentNodeId = def.CurrentNodeId;

                // Remove existing children by querying the DbSets to ensure they have database-generated keys
                var oldNodes = await _context.NodeEntities.Where(n => n.WorkflowDefinitionEntityId == existing.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
                if (oldNodes.Count > 0) _context.NodeEntities.RemoveRange(oldNodes);

                var oldTransitions = await _context.TransitionEntities.Where(t => t.WorkflowDefinitionEntityId == existing.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
                if (oldTransitions.Count > 0) _context.TransitionEntities.RemoveRange(oldTransitions);

                var oldStartPoints = await _context.StartPointEntities.Where(s => s.WorkflowDefinitionEntityId == existing.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
                if (oldStartPoints.Count > 0) _context.StartPointEntities.RemoveRange(oldStartPoints);

                // Clear tracked navigation collections
                existing.Nodes.Clear();
                existing.Transitions.Clear();
                existing.StartPoints.Clear();

                // Persist removals first so we can add fresh children cleanly
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

                if (newNodes.Count > 0) await _context.NodeEntities.AddRangeAsync(newNodes, cancellationToken).ConfigureAwait(false);
                if (newTransitions.Count > 0) await _context.TransitionEntities.AddRangeAsync(newTransitions, cancellationToken).ConfigureAwait(false);
                if (newStartPoints.Count > 0) await _context.StartPointEntities.AddRangeAsync(newStartPoints, cancellationToken).ConfigureAwait(false);

                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                // Reload and return the updated workflow with navigation properties
                var updated = await GetByIdAsync(existing.Id, cancellationToken).ConfigureAwait(false);
                LogUpdated(_logger, def.Id);
                return updated!;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                LogUpdateConcurrency(_logger, ex, def.Id, retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    LogUpdateMaxRetries(_logger, def.Id);
                    throw new InvalidOperationException(
                        $"Unable to update workflow '{def.Id}' after {maxRetries} attempts due to concurrent modifications. Please retry.",
                        ex);
                }

                // Refresh the context to get latest data
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    await entry.ReloadAsync(cancellationToken).ConfigureAwait(false);
                }

                // Small delay before retry with exponential backoff
                await Task.Delay(100 * retryCount, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUpdateError(_logger, ex, def?.Id);
                throw;
            }
        }

        throw new InvalidOperationException($"Unexpected state: exceeded retry loop for workflow '{def.Id}'");
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "DeleteWorkflow", ["WorkflowId"] = id });
        try
        {
            LogDeleting(_logger, id);
            var entity = await _context.WorkflowDefinitions.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
            if (entity == null) return false;
            _context.WorkflowDefinitions.Remove(entity);
            var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogDeleted(_logger, id);
            return affected > 0;
        }
        catch (OperationCanceledException)
        {
            LogDeletingCancelled(_logger, id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            LogDeleteDbError(_logger, ex, id);
            throw;
        }
        catch (Exception ex)
        {
            LogDeleteError(_logger, ex, id);
            throw;
        }
    }

    public async Task<bool> UpdateCurrentNodeIdAsync(string workflowDefinitionId, string? currentNodeId = null, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "UpdateCurrentNodeId", ["WorkflowId"] = workflowDefinitionId, ["NodeId"] = currentNodeId ?? "null" });

        int retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                LogUpdatingCurrentNode(_logger, workflowDefinitionId, currentNodeId, retryCount + 1);

                var existing = await _context.WorkflowDefinitions.FirstOrDefaultAsync(
                    w => w.Id == workflowDefinitionId,
                    cancellationToken).ConfigureAwait(false);

                if (existing == null)
                {
                    LogUpdateCurrentNodeNotFound(_logger, workflowDefinitionId);
                    return false;
                }

                existing.CurrentNodeId = currentNodeId;
                _context.WorkflowDefinitions.Update(existing);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                LogUpdatedCurrentNode(_logger, workflowDefinitionId, currentNodeId);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                LogUpdateCurrentNodeConcurrency(_logger, ex, workflowDefinitionId, retryCount, maxRetries);

                if (retryCount >= maxRetries)
                {
                    LogUpdateCurrentNodeMaxRetries(_logger, workflowDefinitionId);
                    return false; // Return false instead of throwing for this simpler operation
                }

                // Refresh the context to get latest data
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    await entry.ReloadAsync(cancellationToken).ConfigureAwait(false);
                }

                // Small delay before retry with exponential backoff
                await Task.Delay(100 * retryCount, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUpdateCurrentNodeError(_logger, ex, workflowDefinitionId, currentNodeId);
                throw;
            }
        }

        return false;
    }
}
