using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Mediator;

namespace FWH.Common.Workflow.Tests;

public class WorkflowExecutorOptionsTests
{
    public class ScopedDep
    {
        public string Id { get; } = System.Guid.NewGuid().ToString("N");
    }

    public class HandlerUsingDep : IWorkflowActionHandler
    {
        private readonly ScopedDep _dep;
        public HandlerUsingDep(ScopedDep dep) => _dep = dep;
        public string Name => "HandlerUsingDep";

        public Task<IDictionary<string,string>?> HandleAsync(ActionHandlerContext context, IDictionary<string,string> parameters, CancellationToken cancellationToken = default)
        {
            var updates = new Dictionary<string,string> { ["depId"] = _dep.Id };
            return Task.FromResult<IDictionary<string,string>?>(updates);
        }
    }

    class TestLogger<T> : ILogger<T>
    {
        public readonly List<string> Messages = new List<string>();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, System.Func<TState, System.Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private IWorkflowActionExecutor BuildExecutor(IServiceCollection services, WorkflowActionExecutorOptions options, out ServiceProvider sp, out TestLogger<WorkflowActionExecutor> logger)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<NotesDbContext>(o => o.UseSqlite(connection));

        services.AddScoped<ScopedDep>();
        services.AddScoped<HandlerUsingDep>();
        // Expose factory so the registry can create scoped handler instances
        services.AddSingleton<Func<IServiceProvider, IWorkflowActionHandler>>(_ => provider => provider.GetRequiredService<HandlerUsingDep>());

        services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<WorkflowActionHandlerRegistrar>();

        // Register mediator sender and handler (handler uses registry, so tests work with registry path)
        services.AddLogging();
        services.AddSingleton<IMediatorSender, ServiceProviderMediatorSender>();
        services.AddTransient<IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>, WorkflowActionRequestHandler>();

        logger = new TestLogger<WorkflowActionExecutor>();
        services.AddSingleton<ILogger<WorkflowActionExecutor>>(logger);

        services.AddSingleton<IWorkflowActionExecutor>(sp => new WorkflowActionExecutor(sp, sp.GetRequiredService<IMediatorSender>(), Microsoft.Extensions.Options.Options.Create(options), sp.GetRequiredService<ILogger<WorkflowActionExecutor>>()));
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();

        sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<WorkflowActionHandlerRegistrar>();

        return sp.GetRequiredService<IWorkflowActionExecutor>();
    }

    /// <summary>
    /// Tests that when CreateScopeForHandlers is true, each handler execution receives a new scoped dependency instance.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's scoping behavior when CreateScopeForHandlers option is enabled, ensuring handlers get fresh scoped dependencies per execution.</para>
    /// <para><strong>Data involved:</strong> A HandlerUsingDep handler that depends on ScopedDep (registered as scoped). The handler stores the dependency's unique ID in workflow variables. The workflow is executed twice with CreateScopeForHandlers=true. Each execution should create a new service scope, resulting in different ScopedDep instances.</para>
    /// <para><strong>Why the data matters:</strong> Scoped dependencies (e.g., database contexts) should be created fresh for each handler execution to ensure proper isolation and disposal. When CreateScopeForHandlers=true, the executor creates a new service scope per execution, allowing scoped services to be properly resolved and disposed. This prevents state leakage between executions and ensures proper resource cleanup.</para>
    /// <para><strong>Expected outcome:</strong> Both executions should succeed (ok1=true, ok2=true), and the workflow variables should contain different depId values (id1 != id2), confirming different ScopedDep instances were used.</para>
    /// <para><strong>Reason for expectation:</strong> With CreateScopeForHandlers=true, the executor should create a new service scope for each ExecuteAsync call. Each scope resolves a new ScopedDep instance (since it's registered as scoped), resulting in different IDs. The different IDs confirm that scoping works correctly and handlers receive fresh dependencies per execution, which is critical for proper resource management and isolation.</para>
    /// </remarks>
    [Fact]
    public async Task When_CreateScopeForHandlers_True_Then_HandlerGetsNewScopedInstancesPerExecution()
    {
        var services = new ServiceCollection();
        var options = new WorkflowActionExecutorOptions { CreateScopeForHandlers = true, LogExecutionTime = false };
        var executor = BuildExecutor(services, options, out var sp, out var logger);

        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A","A","{\"action\":\"HandlerUsingDep\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        var ok1 = await executor.ExecuteAsync("w", def.Nodes[0], def, CancellationToken.None);
        Assert.True(ok1);
        var vars1 = sp.GetRequiredService<IWorkflowInstanceManager>().GetVariables("w");
        Assert.NotNull(vars1);
        var id1 = vars1["depId"];

        var ok2 = await executor.ExecuteAsync("w", def.Nodes[0], def, CancellationToken.None);
        Assert.True(ok2);
        var vars2 = sp.GetRequiredService<IWorkflowInstanceManager>().GetVariables("w");
        Assert.NotNull(vars2);
        var id2 = vars2["depId"];

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task When_CreateScopeForHandlers_False_Then_HandlerResolvesSameInstanceAcrossExecutions()
    {
        var services = new ServiceCollection();
        var options = new WorkflowActionExecutorOptions { CreateScopeForHandlers = false, LogExecutionTime = false };
        var executor = BuildExecutor(services, options, out var sp, out var logger);

        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A","A","{\"action\":\"HandlerUsingDep\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        var ok1 = await executor.ExecuteAsync("w", def.Nodes[0], def, CancellationToken.None);
        Assert.True(ok1);
        var vars1 = sp.GetRequiredService<IWorkflowInstanceManager>().GetVariables("w");
        Assert.NotNull(vars1);
        var id1 = vars1["depId"];

        var ok2 = await executor.ExecuteAsync("w", def.Nodes[0], def, CancellationToken.None);
        Assert.True(ok2);
        var vars2 = sp.GetRequiredService<IWorkflowInstanceManager>().GetVariables("w");
        Assert.NotNull(vars2);
        var id2 = vars2["depId"];

        Assert.Equal(id1, id2);
    }

    [Fact]
    public async Task When_LogExecutionTime_False_NoTimingLogIsEmitted()
    {
        var services = new ServiceCollection();
        var options = new WorkflowActionExecutorOptions { CreateScopeForHandlers = true, LogExecutionTime = false };
        var executor = BuildExecutor(services, options, out var sp, out var logger);

        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A","A","{\"action\":\"HandlerUsingDep\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        var ok = await executor.ExecuteAsync("w", def.Nodes[0], def, CancellationToken.None);
        Assert.True(ok);

        // Ensure no messages mention "handled by" (the timing log)
        Assert.DoesNotContain(logger.Messages, m => m.Contains("handled by"));
    }

    [Fact]
    public async Task When_LogExecutionTime_True_TimingLogIsEmitted()
    {
        var services = new ServiceCollection();
        var options = new WorkflowActionExecutorOptions { CreateScopeForHandlers = true, LogExecutionTime = true };
        var executor = BuildExecutor(services, options, out var sp, out var logger);

        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A","A","{\"action\":\"HandlerUsingDep\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        var ok = await executor.ExecuteAsync("w", def.Nodes[0], def, CancellationToken.None);
        Assert.True(ok);

        Assert.Contains(logger.Messages, m => m.Contains("handled by") || m.Contains("Handled by"));
    }
}
