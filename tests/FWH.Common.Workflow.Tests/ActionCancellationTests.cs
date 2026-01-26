using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Instance;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Workflow.Tests;

public class ActionCancellationTests
{
    /// <summary>
    /// Tests that workflow action handlers can be cancelled via CancellationToken and execution stops gracefully.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowActionExecutor.ExecuteAsync method's cancellation support when a CancellationToken is cancelled during handler execution.</para>
    /// <para><strong>Data involved:</strong> A WorkflowActionHandlerAdapter for "LongRunning" action that performs a 5-second delay, honoring the CancellationToken. A CancellationTokenSource with a 100ms timeout is used to cancel the execution before the delay completes. The workflow definition contains a single node with the LongRunning action.</para>
    /// <para><strong>Why the data matters:</strong> Long-running actions (e.g., API calls, file operations) may need to be cancelled if the user navigates away or the workflow is stopped. The handler must respect cancellation tokens to allow graceful termination. The 100ms timeout ensures cancellation happens before the 5-second delay completes, testing that cancellation is actually honored.</para>
    /// <para><strong>Expected outcome:</strong> ExecuteAsync should return false (indicating execution was cancelled) when the CancellationToken is cancelled before the handler completes.</para>
    /// <para><strong>Reason for expectation:</strong> When a CancellationToken is cancelled, the handler's Task.Delay should throw OperationCanceledException, causing ExecuteAsync to return false. This allows the workflow system to detect cancelled executions and handle them appropriately (e.g., not updating workflow state, logging cancellation). The false return value indicates the action did not complete successfully.</para>
    /// </remarks>
    [Fact]
    public async Task HandlerCanBeCancelled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();

        // create handler that honors cancellation
        var handler = new WorkflowActionHandlerAdapter("LongRunning", async (ctx, p, ct) =>
        {
            await Task.Delay(5000, ct).ConfigureAwait(true);
            return new Dictionary<string, string> { ["ok"] = "1" };
        });

        services.AddSingleton<IWorkflowActionHandler>(handler);
        services.AddSingleton<WorkflowActionHandlerRegistrar>();
        services.AddLogging();
        services.AddSingleton<IMediatorSender, ServiceProviderMediatorSender>();
        services.AddTransient<IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>, WorkflowActionRequestHandler>();
        services.AddSingleton<IWorkflowActionExecutor, WorkflowActionExecutor>();

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<WorkflowActionHandlerRegistrar>();

        var executor = sp.GetRequiredService<IWorkflowActionExecutor>();
        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A", "A", "{\"action\":\"LongRunning\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        using var cts = new CancellationTokenSource(100);
        var result = await executor.ExecuteAsync("w", def.Nodes[0], def, cts.Token).ConfigureAwait(true);
        Assert.False(result);
    }
}
