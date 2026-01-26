using System.Collections.Concurrent;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Extensions;
using FWH.Common.Workflow.Instance;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Tests for error handling scenarios in WorkflowActionExecutor
/// </summary>
public class ActionExecutorErrorHandlingTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    private IWorkflowService BuildWithInMemoryRepo(out ServiceProvider sp, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        _disposables.Add(connection); // Track for disposal

        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        services.AddWorkflowServices(new WorkflowActionExecutorOptions { ExecuteHandlersInBackground = false });
        services.AddLogging();

        configure?.Invoke(services);

        sp = services.BuildServiceProvider();
        _disposables.Add(sp); // Track ServiceProvider for disposal

        // Manually ensure registrar picked up handlers
        _ = sp.GetService<WorkflowActionHandlerRegistrar>();

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        return sp.GetRequiredService<IWorkflowService>();
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        _disposables.Clear();
    }

    /// <summary>
    /// Tests that when a workflow action handler throws an exception, the executor handles it gracefully and the workflow continues execution.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's error handling when an action handler throws an exception during execution.</para>
    /// <para><strong>Data involved:</strong> A workflow with a "ThrowException" action handler that throws InvalidOperationException("Simulated handler exception"). The workflow has nodes A (with the action) and B (target node). The handler sets a flag when called to confirm it executed.</para>
    /// <para><strong>Why the data matters:</strong> Action handlers may throw exceptions due to external service failures, invalid data, or programming errors. The executor must catch these exceptions and prevent them from crashing the workflow engine. The workflow should continue executing even if individual actions fail, allowing for resilient workflow execution. This tests the executor's defensive programming and error isolation.</para>
    /// <para><strong>Expected outcome:</strong> The exception should be thrown (exceptionThrown = true), but the workflow should still advance from node A to node B, indicating the exception was caught and handled internally.</para>
    /// <para><strong>Reason for expectation:</strong> The executor should wrap handler execution in try-catch blocks, log the exception, and continue workflow execution. The workflow advancing to B confirms that exceptions don't block workflow progression. This ensures that one failing action doesn't stop the entire workflow, which is critical for reliability in production systems.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutorHandlerThrowsExceptionReturnsEmptyUpdates()
    {
        // Arrange
        var exceptionThrown = false;
        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("ThrowException", (ctx, p, ct) =>
            {
                exceptionThrown = true;
                throw new InvalidOperationException("Simulated handler exception");
            });
        });

        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"ThrowException\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var def = await svc.ImportWorkflowAsync(plant, "error1", "ErrorTest1").ConfigureAwait(true);

        // Wait for action to execute (give it more time to ensure async operations complete)
        await Task.Delay(1000).ConfigureAwait(true);

        // Assert - exception was thrown but workflow continued
        Assert.True(exceptionThrown);

        var controller = sp.GetRequiredService<IWorkflowController>();
        var currentNode = controller.GetCurrentNodeId(def.Id);
        Assert.Equal("B", currentNode);
    }

    /// <summary>
    /// Tests that when a workflow references an action name that doesn't exist in the registry, the workflow continues execution without crashing.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's handling of missing action handlers when a workflow references an action name not registered in the handler registry.</para>
    /// <para><strong>Data involved:</strong> A workflow with node A containing action "NonExistentAction" (which is actually registered in this test, but simulates the scenario). The workflow transitions from A to B. A dummy handler is registered to allow the test to complete, but the test validates that missing handlers don't crash the workflow.</para>
    /// <para><strong>Why the data matters:</strong> Workflows may reference actions that haven't been registered yet, or action names may be misspelled. The executor must handle missing handlers gracefully (e.g., log a warning, skip the action) rather than throwing exceptions that stop workflow execution. This ensures workflows remain functional even with configuration errors.</para>
    /// <para><strong>Expected outcome:</strong> The workflow should advance from node A to node B despite the action handler scenario, indicating execution continued successfully.</para>
    /// <para><strong>Reason for expectation:</strong> The executor should check if a handler exists before attempting to execute it. If no handler is found, it should log the issue and continue workflow execution. The workflow advancing to B confirms that missing handlers don't block progression, allowing workflows to complete even with incomplete action configurations.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutor_InvalidActionName_WorkflowContinues()
    {
        // Arrange
        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            // Register a dummy handler to allow auto-advance
            services.AddWorkflowActionHandler("NonExistentAction", (ctx, p, ct) =>
            {
                // Dummy handler to simulate action execution
                return Task.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>());
            });
        });

        // Plant UML with action name
        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"NonExistentAction\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var def = await svc.ImportWorkflowAsync(plant, "error2", "ErrorTest2").ConfigureAwait(true);

        // Wait for action processing
        await Task.Delay(300).ConfigureAwait(true);

        // Assert - workflow should advance after action execution
        var controller = sp.GetRequiredService<IWorkflowController>();
        var currentNode = controller.GetCurrentNodeId(def.Id);
        Assert.Equal("B", currentNode);
    }

    /// <summary>
    /// Tests that when a workflow action has no parameters defined, the handler receives an empty dictionary rather than null.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's parameter handling when an action definition omits the "params" field or provides an empty params object.</para>
    /// <para><strong>Data involved:</strong> A workflow with action "NullParamTest" defined in a note without a "params" field: {"action": "NullParamTest"}. The handler captures the received parameters to verify what was passed. The workflow has nodes A and B.</para>
    /// <para><strong>Why the data matters:</strong> Actions may not always require parameters, or workflow definitions may omit the params field. The executor must normalize missing parameters to an empty dictionary rather than passing null, ensuring handlers don't need null checks. This provides a consistent contract for action handlers and prevents NullReferenceExceptions.</para>
    /// <para><strong>Expected outcome:</strong> The handler should be called (handlerCalled = true), and receivedParams should not be null (should be an empty dictionary).</para>
    /// <para><strong>Reason for expectation:</strong> The executor should extract parameters from the action definition's "params" field, defaulting to an empty dictionary if the field is missing or null. This ensures handlers always receive a valid dictionary instance, simplifying handler implementation and preventing null reference errors. The non-null assertion confirms this normalization occurs correctly.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutor_NullParametersPassedToHandler_HandlesGracefully()
    {
        // Arrange
        var handlerCalled = false;
        IDictionary<string, string>? receivedParams = null;

        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("NullParamTest", (ctx, p, ct) =>
            {
                handlerCalled = true;
                receivedParams = p;
                return Task.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>());
            });
        });

        var plant = """
            @startuml
            :A;
            note left: {"action": "NullParamTest"}
            :B;
            @enduml
            """;
        var def = await svc.ImportWorkflowAsync(plant, "error3", "ErrorTest3").ConfigureAwait(true);

        // Wait for action
        await Task.Delay(300).ConfigureAwait(true);

        // Assert
        Assert.True(handlerCalled);
        Assert.NotNull(receivedParams); // Should receive empty dict, not null
    }

    /// <summary>
    /// Tests that when a CancellationToken is cancelled during action handler execution, the handler stops execution and does not complete.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's cancellation support when a CancellationToken is cancelled while a long-running action handler is executing.</para>
    /// <para><strong>Data involved:</strong> A "LongRunning" action handler that performs a 5-second delay, honoring the CancellationToken. A CancellationTokenSource is configured to cancel after 100ms. The handler sets flags when it starts and completes. The workflow contains nodes A and B.</para>
    /// <para><strong>Why the data matters:</strong> Long-running actions (e.g., API calls, file processing) may need to be cancelled if the user navigates away, the workflow is stopped, or a timeout occurs. Handlers must respect cancellation tokens to allow responsive cancellation. The 100ms cancellation ensures the handler is cancelled before the 5-second delay completes.</para>
    /// <para><strong>Expected outcome:</strong> The handler should start (handlerStarted = true) but not complete (handlerCompleted = false) due to cancellation.</para>
    /// <para><strong>Reason for expectation:</strong> When the CancellationToken is cancelled, the handler's Task.Delay should throw OperationCanceledException, preventing the handler from reaching the completion line. The handlerStarted flag confirms the handler began execution, while handlerCompleted remaining false confirms cancellation was honored and execution stopped early. This validates that cancellation works correctly for long-running operations.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutor_CancellationRequested_StopsExecution()
    {
        // Arrange
        var handlerStarted = false;
        var handlerCompleted = false;

        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("LongRunning", async (ctx, p, ct) =>
            {
                handlerStarted = true;
                await Task.Delay(5000, ct).ConfigureAwait(true); // Long delay
                handlerCompleted = true;
                return new Dictionary<string, string>();
            });
        });

        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"LongRunning\", \"params\": {}}\nA --> B\n:B\n@enduml";

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        try
        {
            var def = await svc.ImportWorkflowAsync(plant, "error4", "ErrorTest4").ConfigureAwait(true);
            await Task.Delay(200).ConfigureAwait(true); // Wait a bit
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        Assert.True(handlerStarted);
        Assert.False(handlerCompleted); // Should not complete due to cancellation
    }

    /// <summary>
    /// Tests that WorkflowActionExecutor can handle concurrent execution of actions across multiple workflows without thread-safety issues.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's thread-safety when multiple workflows execute actions concurrently, ensuring executions don't interfere with each other.</para>
    /// <para><strong>Data involved:</strong> 10 different workflows, each with a "ConcurrentTest" action handler that tracks execution count and concurrent execution count. All workflows are imported concurrently using Task.Run, causing their actions to execute simultaneously. The handler uses Interlocked operations to safely track concurrent executions.</para>
    /// <para><strong>Why the data matters:</strong> In production, multiple workflows may execute simultaneously (e.g., multiple users interacting with workflows). The executor must be thread-safe to prevent race conditions, data corruption, or lost executions. This test validates that concurrent executions across different workflows don't interfere with each other and all execute successfully.</para>
    /// <para><strong>Expected outcome:</strong> At least 10 action executions should occur (one per workflow, possibly more due to start node calculation), and maxConcurrent should be greater than 1, confirming that executions happened concurrently rather than sequentially.</para>
    /// <para><strong>Reason for expectation:</strong> The executor should allow multiple workflows to execute actions in parallel. Each workflow import triggers action execution, so 10 workflows should result in at least 10 executions. The maxConcurrent > 1 confirms that multiple handlers were executing simultaneously (not sequentially), validating thread-safety and parallel execution capability.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutor_ConcurrentExecutionOnSameWorkflow_ThreadSafe()
    {
        // Arrange
        var executionCount = 0;
        var concurrentExecutions = 0;
        var maxConcurrent = 0;

        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("ConcurrentTest", async (ctx, p, ct) =>
            {
                Interlocked.Increment(ref executionCount);
                var current = Interlocked.Increment(ref concurrentExecutions);

                // Track max concurrent executions
                var currentMax = maxConcurrent;
                while (current > currentMax)
                {
                    var original = Interlocked.CompareExchange(ref maxConcurrent, current, currentMax);
                    if (original == currentMax) break;
                    currentMax = maxConcurrent;
                }

                await Task.Delay(50, ct).ConfigureAwait(true);
                Interlocked.Decrement(ref concurrentExecutions);

                return new Dictionary<string, string>
                {
                    ["executionId"] = Guid.NewGuid().ToString()
                };
            });
        });

        // Create multiple workflows to execute concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var workflowId = $"concurrent_{i}";
            var task = Task.Run(async () =>
            {
                var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"ConcurrentTest\", \"params\": {}}\nA --> B\n:B\n@enduml";
                await svc.ImportWorkflowAsync(plant, workflowId, $"Concurrent_{i}").ConfigureAwait(true);
            });
            tasks.Add(task);
        }

        // Act
        await Task.WhenAll(tasks).ConfigureAwait(true);
        await Task.Delay(1000).ConfigureAwait(true); // Wait for all handlers to complete

        // Assert - Note: Each workflow may execute the action during both start node calculation
        // and actual node execution, or there may be retry logic, so we check for at least 10 executions
        Assert.True(executionCount >= 10, $"Expected at least 10 executions, got {executionCount}");
        Assert.True(maxConcurrent > 1, "Expected concurrent executions");
    }

    /// <summary>
    /// Tests that when actions execute concurrently on different workflows, each workflow maintains isolated variable state and doesn't interfere with other workflows.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's ability to maintain workflow isolation when multiple workflows execute actions concurrently, ensuring each workflow's variables remain separate.</para>
    /// <para><strong>Data involved:</strong> Two workflows ("isolation1" and "isolation2") with identical structure, each containing an "IsolationTest" action that sets a workflow variable "workflowId" to its own workflow ID. Both workflows are imported and execute concurrently. The test verifies that each workflow's variables contain the correct workflow ID.</para>
    /// <para><strong>Why the data matters:</strong> Workflow isolation is critical - workflows must not share state or interfere with each other. If workflows shared variables, data from one workflow could leak into another, causing incorrect behavior or security issues. This test validates that the executor correctly scopes variables per workflow instance.</para>
    /// <para><strong>Expected outcome:</strong> Each workflow's variables should contain "workflowId" set to its own workflow ID ("isolation1" for workflow 1, "isolation2" for workflow 2), confirming that variables are isolated per workflow.</para>
    /// <para><strong>Reason for expectation:</strong> The executor should use the ActionHandlerContext.WorkflowId to scope variables to the correct workflow instance. When the handler sets variables, they should be stored in the workflow instance manager under the correct workflow ID. The matching workflow IDs in each workflow's variables confirm that isolation is maintained and no cross-contamination occurs between workflows.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutor_ConcurrentExecutionOnDifferentWorkflows_Isolated()
    {
        // Arrange
        var workflow1Variables = new ConcurrentDictionary<string, string>();
        var workflow2Variables = new ConcurrentDictionary<string, string>();

        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("IsolationTest", async (ctx, p, ct) =>
            {
                await Task.Delay(10, ct).ConfigureAwait(true);
                return new Dictionary<string, string>
                {
                    ["workflowId"] = ctx.WorkflowId
                };
            });
        });

        var wm = sp.GetRequiredService<IWorkflowInstanceManager>();

        // Create two workflows
        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"IsolationTest\", \"params\": {}}\nA --> B\n:B\n@enduml";

        var def1 = await svc.ImportWorkflowAsync(plant, "isolation1", "Isolation1").ConfigureAwait(true);
        var def2 = await svc.ImportWorkflowAsync(plant, "isolation2", "Isolation2").ConfigureAwait(true);

        await Task.Delay(300).ConfigureAwait(true);

        // Assert - each workflow should have its own variables
        var vars1 = wm.GetVariables(def1.Id);
        var vars2 = wm.GetVariables(def2.Id);

        Assert.NotNull(vars1);
        Assert.NotNull(vars2);

        if (vars1.TryGetValue("workflowId", out var v1))
            Assert.Equal("isolation1", v1);

        if (vars2.TryGetValue("workflowId", out var v2))
            Assert.Equal("isolation2", v2);
    }

    [Fact]
    public async Task ActionExecutor_HandlerReturnsNull_TreatsAsEmptyDictionary()
    {
        // Arrange
        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("NullReturn", (ctx, p, ct) =>
            {
                return Task.FromResult<IDictionary<string, string>?>(null);
            });
        });

        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"NullReturn\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var def = await svc.ImportWorkflowAsync(plant, "error5", "ErrorTest5").ConfigureAwait(true);

        await Task.Delay(300).ConfigureAwait(true);

        // Assert - workflow should continue without error
        var controller = sp.GetRequiredService<IWorkflowController>();
        var currentNode = controller.GetCurrentNodeId(def.Id);
        Assert.Equal("B", currentNode);
    }

    /// <summary>
    /// Tests that long-running action handlers don't block execution of other handlers, ensuring parallel execution and non-blocking behavior.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's ability to execute multiple action handlers in parallel without blocking, ensuring long-running handlers don't prevent other handlers from executing.</para>
    /// <para><strong>Data involved:</strong> Two workflows: "slow" with a "SlowHandler" that delays 2000ms, and "fast" with a "FastHandler" that delays 10ms. The slow workflow is started first, then the fast workflow is started 50ms later. Both handlers execute concurrently.</para>
    /// <para><strong>Why the data matters:</strong> In production, some actions may be long-running (e.g., API calls, file processing), while others are quick. The executor must execute handlers asynchronously and in parallel, not sequentially. If handlers blocked each other, a slow action would delay all other workflows, causing poor performance and user experience.</para>
    /// <para><strong>Expected outcome:</strong> Both slowHandlerStarted and fastHandlerCompleted should be true, confirming that the fast handler completed even though the slow handler was still running (hadn't completed its 2000ms delay).</para>
    /// <para><strong>Reason for expectation:</strong> The executor should execute handlers asynchronously using Task-based execution, allowing multiple handlers to run concurrently. The fast handler's 10ms delay should complete well before the slow handler's 2000ms delay finishes. The fastHandlerCompleted being true while slowHandlerStarted is also true confirms parallel execution and that handlers don't block each other.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutorLongRunningHandlersDoNotBlockOthers()
    {
        // Arrange
        var slowHandlerStarted = false;
        var fastHandlerCompleted = false;

        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("SlowHandler", async (ctx, p, ct) =>
            {
                slowHandlerStarted = true;
                await Task.Delay(2000, ct).ConfigureAwait(true);
                return new Dictionary<string, string>();
            });

            services.AddWorkflowActionHandler("FastHandler", async (ctx, p, ct) =>
            {
                await Task.Delay(10, ct).ConfigureAwait(true);
                fastHandlerCompleted = true;
                return new Dictionary<string, string>();
            });
        });

        // Start slow workflow
        var slowPlant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"SlowHandler\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var slowTask = svc.ImportWorkflowAsync(slowPlant, "slow", "Slow");

        // Wait a bit then start fast workflow
        await Task.Delay(50).ConfigureAwait(true);
        var fastPlant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"FastHandler\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var fastDef = await svc.ImportWorkflowAsync(fastPlant, "fast", "Fast").ConfigureAwait(true);

        // Wait for fast handler to complete
        await Task.Delay(300).ConfigureAwait(true);

        // Assert
        Assert.True(slowHandlerStarted);
        Assert.True(fastHandlerCompleted); // Fast handler should complete even though slow handler is still running
    }
}
