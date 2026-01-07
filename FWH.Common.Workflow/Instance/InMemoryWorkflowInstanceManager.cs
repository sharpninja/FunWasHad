using System;
using System.Collections.Concurrent;

namespace FWH.Common.Workflow.Instance;

/// <summary>
/// In-memory implementation of workflow instance state management.
/// Single Responsibility: Track current node for workflow instances in memory.
/// </summary>
public class InMemoryWorkflowInstanceManager : IWorkflowInstanceManager
{
    private readonly ConcurrentDictionary<string, string?> _currentNodeByWorkflow = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string,string>> _vars = new(StringComparer.Ordinal);

    public string? GetCurrentNode(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) return null;
        return _currentNodeByWorkflow.TryGetValue(workflowId, out var node) ? node : null;
    }

    public void SetCurrentNode(string workflowId, string? nodeId)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) 
            throw new ArgumentNullException(nameof(workflowId));
        
        _currentNodeByWorkflow[workflowId] = nodeId;
    }

    public void ClearCurrentNode(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) return;
        _currentNodeByWorkflow.TryRemove(workflowId, out _);
    }

    public IDictionary<string,string>? GetVariables(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) return null;
        if (!_vars.TryGetValue(workflowId, out var m))
        {
            m = new ConcurrentDictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            _vars[workflowId] = m;
        }

        return m;
    }

    public void SetVariable(string workflowId, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) return;
        if (string.IsNullOrWhiteSpace(key)) return;

        var m = GetVariables(workflowId)!;
        m[key] = value;
    }
}
