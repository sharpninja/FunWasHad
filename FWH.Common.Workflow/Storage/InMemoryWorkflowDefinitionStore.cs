using System;
using System.Collections.Generic;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow.Storage;

/// <summary>
/// In-memory implementation of workflow definition storage.
/// Single Responsibility: Store workflow definitions in memory.
/// </summary>
public class InMemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions = new(StringComparer.Ordinal);

    public void Store(WorkflowDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        _definitions[definition.Id] = definition;
    }

    public WorkflowDefinition? Get(string workflowId)
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
