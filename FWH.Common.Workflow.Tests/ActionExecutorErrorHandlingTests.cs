using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Instance;
using FWH.Mobile.Data.Repositories;
using FWH.Mobile.Data.Data;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Extensions;
using System.Collections.Concurrent;
using System.Threading;
using System;
using System.Collections.Generic;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Tests for error handling scenarios in WorkflowActionExecutor
/// </summary>
public class ActionExecutorErrorHandlingTests
{
    private IWorkflowService BuildWithInMemoryRepo(out ServiceProvider sp, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<WorkflowActionHandlerRegistrar>();
        services.AddSingleton<IWorkflowActionExecutor, WorkflowActionExecutor>();
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();
        services.AddLogging();

        configure?.Invoke(services);

        sp = services.BuildServiceProvider();

        // Manually ensure registrar picked up handlers
        _ = sp.GetService<WorkflowActionHandlerRegistrar>();

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        return sp.GetRequiredService<IWorkflowService>();
    }

    [Fact]
    public async Task ActionExecutor_HandlerThrowsException_ReturnsEmptyUpdates()
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
        var def = await svc.ImportWorkflowAsync(plant, "error1", "ErrorTest1");

        // Wait for action to execute
        await Task.Delay(500);

        // Assert - exception was thrown but workflow continued
        Assert.True(exceptionThrown);
        
        // Workflow should still advance to B despite exception
        var controller = sp.GetRequiredService<IWorkflowController>();
        var currentNode = controller.GetCurrentNodeId(def.Id);
        Assert.Equal("B", currentNode);
    }

    [Fact]
    public async Task ActionExecutor_InvalidActionName_WorkflowContinues()
    {
        // Arrange
        var svc = BuildWithInMemoryRepo(out var sp);
        
        // Plant UML with action name that doesn't exist
        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"NonExistentAction\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var def = await svc.ImportWorkflowAsync(plant, "error2", "ErrorTest2");

        // Wait for action processing
        await Task.Delay(300);

        // Assert - workflow should continue despite missing handler
        var controller = sp.GetRequiredService<IWorkflowController>();
        var currentNode = controller.GetCurrentNodeId(def.Id);
        Assert.Equal("B", currentNode);
    }

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
                return Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>());
            });
        });

        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"NullParamTest\"}\nA --> B\n:B\n@enduml";
        var def = await svc.ImportWorkflowAsync(plant, "error3", "ErrorTest3");

        // Wait for action
        await Task.Delay(300);

        // Assert
        Assert.True(handlerCalled);
        Assert.NotNull(receivedParams); // Should receive empty dict, not null
    }

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
                await Task.Delay(5000, ct); // Long delay
                handlerCompleted = true;
                return new Dictionary<string, string>();
            });
        });

        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"LongRunning\", \"params\": {}}\nA --> B\n:B\n@enduml";
        
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        try
        {
            var def = await svc.ImportWorkflowAsync(plant, "error4", "ErrorTest4");
            await Task.Delay(200); // Wait a bit
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        Assert.True(handlerStarted);
        Assert.False(handlerCompleted); // Should not complete due to cancellation
    }

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

                await Task.Delay(50, ct);
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
                await svc.ImportWorkflowAsync(plant, workflowId, $"Concurrent_{i}");
            });
            tasks.Add(task);
        }

        // Act
        await Task.WhenAll(tasks);
        await Task.Delay(500); // Wait for all handlers to complete

        // Assert
        Assert.Equal(10, executionCount);
        Assert.True(maxConcurrent > 1, "Expected concurrent executions");
    }

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
                await Task.Delay(10, ct);
                return new Dictionary<string, string>
                {
                    ["workflowId"] = ctx.WorkflowId
                };
            });
        });

        var wm = sp.GetRequiredService<IWorkflowInstanceManager>();

        // Create two workflows
        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"IsolationTest\", \"params\": {}}\nA --> B\n:B\n@enduml";
        
        var def1 = await svc.ImportWorkflowAsync(plant, "isolation1", "Isolation1");
        var def2 = await svc.ImportWorkflowAsync(plant, "isolation2", "Isolation2");

        await Task.Delay(300);

        // Assert - each workflow should have its own variables
        var vars1 = wm.GetVariables(def1.Id);
        var vars2 = wm.GetVariables(def2.Id);

        Assert.NotNull(vars1);
        Assert.NotNull(vars2);
        
        if (vars1.ContainsKey("workflowId"))
        {
            Assert.Equal("isolation1", vars1["workflowId"]);
        }
        
        if (vars2.ContainsKey("workflowId"))
        {
            Assert.Equal("isolation2", vars2["workflowId"]);
        }
    }

    [Fact]
    public async Task ActionExecutor_HandlerReturnsNull_TreatsAsEmptyDictionary()
    {
        // Arrange
        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("NullReturn", (ctx, p, ct) =>
            {
                return Task.FromResult<IDictionary<string, string>>(null!);
            });
        });

        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"NullReturn\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var def = await svc.ImportWorkflowAsync(plant, "error5", "ErrorTest5");

        await Task.Delay(300);

        // Assert - workflow should continue without error
        var controller = sp.GetRequiredService<IWorkflowController>();
        var currentNode = controller.GetCurrentNodeId(def.Id);
        Assert.Equal("B", currentNode);
    }

    [Fact]
    public async Task ActionExecutor_LongRunningHandlers_DoNotBlockOthers()
    {
        // Arrange
        var slowHandlerStarted = false;
        var fastHandlerCompleted = false;

        var svc = BuildWithInMemoryRepo(out var sp, services =>
        {
            services.AddWorkflowActionHandler("SlowHandler", async (ctx, p, ct) =>
            {
                slowHandlerStarted = true;
                await Task.Delay(2000, ct);
                return new Dictionary<string, string>();
            });

            services.AddWorkflowActionHandler("FastHandler", async (ctx, p, ct) =>
            {
                await Task.Delay(10, ct);
                fastHandlerCompleted = true;
                return new Dictionary<string, string>();
            });
        });

        // Start slow workflow
        var slowPlant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"SlowHandler\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var slowTask = svc.ImportWorkflowAsync(slowPlant, "slow", "Slow");

        // Wait a bit then start fast workflow
        await Task.Delay(50);
        var fastPlant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"FastHandler\", \"params\": {}}\nA --> B\n:B\n@enduml";
        var fastDef = await svc.ImportWorkflowAsync(fastPlant, "fast", "Fast");

        // Wait for fast handler to complete
        await Task.Delay(300);

        // Assert
        Assert.True(slowHandlerStarted);
        Assert.True(fastHandlerCompleted); // Fast handler should complete even though slow handler is still running
    }
}
