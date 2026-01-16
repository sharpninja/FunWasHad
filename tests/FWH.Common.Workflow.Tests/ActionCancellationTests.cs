using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Instance;
using System.Collections.Generic;

namespace FWH.Common.Workflow.Tests;

public class ActionCancellationTests
{
    [Fact]
    public async Task Handler_CanBeCancelled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();

        // create handler that honors cancellation
        var handler = new WorkflowActionHandlerAdapter("LongRunning", async (ctx, p, ct) =>
        {
            await Task.Delay(5000, ct);
            return new Dictionary<string,string> { ["ok"] = "1" };
        });

        services.AddSingleton<IWorkflowActionHandler>(handler);
        services.AddSingleton<WorkflowActionHandlerRegistrar>();
        services.AddSingleton<IWorkflowActionExecutor, WorkflowActionExecutor>();

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<WorkflowActionHandlerRegistrar>();

        var executor = sp.GetRequiredService<IWorkflowActionExecutor>();
        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w","n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A","A","{\"action\":\"LongRunning\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        using var cts = new CancellationTokenSource(100);
        var result = await executor.ExecuteAsync("w", def.Nodes[0], def, cts.Token);
        Assert.False(result);
    }
}
