using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Data.Repositories;

public partial class EfWorkflowRepository
{
    [LoggerMessage(LogLevel.Debug, "Creating workflow {WorkflowId}")]
    private static partial void LogCreating(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Warning, "Workflow {WorkflowId} already exists, cannot create. Use UpdateAsync instead.")]
    private static partial void LogAlreadyExists(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Created workflow {WorkflowId}")]
    private static partial void LogCreated(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Warning, "Unique constraint violation when creating workflow {WorkflowId}. Workflow already exists.")]
    private static partial void LogUniqueConstraintCreate(ILogger logger, Exception ex, string? workflowId);

    [LoggerMessage(LogLevel.Error, "Error creating workflow {WorkflowId}")]
    private static partial void LogCreateError(ILogger logger, Exception ex, string? workflowId);

    [LoggerMessage(LogLevel.Debug, "Loading workflow {WorkflowId}")]
    private static partial void LogLoading(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Loading workflow {WorkflowId} was cancelled")]
    private static partial void LogLoadingCancelled(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Database error loading workflow {WorkflowId}")]
    private static partial void LogLoadDbError(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Error, "Error loading workflow {WorkflowId}")]
    private static partial void LogLoadError(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Loading all workflows")]
    private static partial void LogLoadingAll(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Loading all workflows was cancelled")]
    private static partial void LogLoadingAllCancelled(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Database error loading all workflows")]
    private static partial void LogLoadAllDbError(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error loading all workflows")]
    private static partial void LogLoadAllError(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Finding workflows matching pattern {Pattern} since {Since}")]
    private static partial void LogFindByPattern(ILogger logger, string pattern, DateTimeOffset since);

    [LoggerMessage(LogLevel.Information, "Finding workflows by pattern {Pattern} was cancelled")]
    private static partial void LogFindByPatternCancelled(ILogger logger, string pattern);

    [LoggerMessage(LogLevel.Error, "Database error finding workflows by name pattern {Pattern}")]
    private static partial void LogFindByPatternDbError(ILogger logger, Exception ex, string pattern);

    [LoggerMessage(LogLevel.Error, "Error finding workflows by name pattern {Pattern}")]
    private static partial void LogFindByPatternError(ILogger logger, Exception ex, string pattern);

    [LoggerMessage(LogLevel.Debug, "Updating workflow {WorkflowId} (attempt {RetryCount})")]
    private static partial void LogUpdating(ILogger logger, string workflowId, int retryCount);

    [LoggerMessage(LogLevel.Warning, "Workflow {WorkflowId} not found for update")]
    private static partial void LogUpdateNotFound(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Updated workflow {WorkflowId}")]
    private static partial void LogUpdated(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Warning, "Concurrency conflict updating workflow {WorkflowId}, attempt {RetryCount}/{MaxRetries}")]
    private static partial void LogUpdateConcurrency(ILogger logger, Exception ex, string workflowId, int retryCount, int maxRetries);

    [LoggerMessage(LogLevel.Error, "Max retries exceeded for workflow {WorkflowId} due to concurrency conflicts")]
    private static partial void LogUpdateMaxRetries(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Error updating workflow {WorkflowId}")]
    private static partial void LogUpdateError(ILogger logger, Exception ex, string? workflowId);

    [LoggerMessage(LogLevel.Debug, "Deleting workflow {WorkflowId}")]
    private static partial void LogDeleting(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Deleted workflow {WorkflowId}")]
    private static partial void LogDeleted(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Deleting workflow {WorkflowId} was cancelled")]
    private static partial void LogDeletingCancelled(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Database error deleting workflow {WorkflowId}")]
    private static partial void LogDeleteDbError(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Error, "Error deleting workflow {WorkflowId}")]
    private static partial void LogDeleteError(ILogger logger, Exception ex, string workflowId);

    [LoggerMessage(LogLevel.Debug, "Updating CurrentNodeId for {WorkflowId} to {NodeId} (attempt {RetryCount})")]
    private static partial void LogUpdatingCurrentNode(ILogger logger, string workflowId, string? nodeId, int retryCount);

    [LoggerMessage(LogLevel.Warning, "Workflow {WorkflowId} not found when updating CurrentNodeId")]
    private static partial void LogUpdateCurrentNodeNotFound(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Information, "Updated CurrentNodeId for {WorkflowId} to {NodeId}")]
    private static partial void LogUpdatedCurrentNode(ILogger logger, string workflowId, string? nodeId);

    [LoggerMessage(LogLevel.Warning, "Concurrency conflict updating CurrentNodeId for {WorkflowId}, attempt {RetryCount}/{MaxRetries}")]
    private static partial void LogUpdateCurrentNodeConcurrency(ILogger logger, Exception ex, string workflowId, int retryCount, int maxRetries);

    [LoggerMessage(LogLevel.Error, "Max retries exceeded for workflow {WorkflowId} CurrentNodeId update due to concurrency conflicts")]
    private static partial void LogUpdateCurrentNodeMaxRetries(ILogger logger, string workflowId);

    [LoggerMessage(LogLevel.Error, "Error updating CurrentNodeId for {WorkflowId} to {NodeId}")]
    private static partial void LogUpdateCurrentNodeError(ILogger logger, Exception ex, string workflowId, string? nodeId);
}
