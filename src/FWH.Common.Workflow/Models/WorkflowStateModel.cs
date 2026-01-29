using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Common.Workflow.Models;

public partial class WorkflowStateModel : ObservableObject
{
    private ConcurrentDictionary<string, WorkflowModel> Workflows = new();

    public bool TryGetWorkflow(string id, out WorkflowModel? model) => Workflows.TryGetValue(id, out model);

    public void AddOrUpdateWorkflow(string id, WorkflowModel model) => Workflows[id] = model;
}
