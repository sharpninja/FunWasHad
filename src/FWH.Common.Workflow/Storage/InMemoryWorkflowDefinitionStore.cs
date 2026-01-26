using System.Collections.Concurrent;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow.Storage;

/// <summary>
/// Thread-safe in-memory implementation of workflow definition storage.
/// Single Responsibility: Store workflow definitions in memory with concurrent access support.
/// </summary>
public class InMemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new(StringComparer.Ordinal);

    public void Store(WorkflowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Id] = definition;
    }

    public WorkflowDefinition? GetById(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) return null;
        return _definitions.TryGetValue(workflowId, out var def) ? def : null;
    }

    public bool Exists(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) return false;
        return _definitions.ContainsKey(workflowId);
    }
}
