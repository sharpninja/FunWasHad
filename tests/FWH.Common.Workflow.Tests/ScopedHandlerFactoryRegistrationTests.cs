using System.Collections.Concurrent;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Extensions;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Views;
using FWH.Mobile.Data.Data;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Workflow.Tests;

public class ScopedHandlerFactoryRegistrationTests
{
    public class Dep
    {
        public string Value => "dep-value";
    }

    public class HandlerWithDep : IWorkflowActionHandler
    {
        private readonly Dep _dep;
        public HandlerWithDep(Dep dep) => _dep = dep;
        public string Name => "HandlerWithDep";
        public Task<IDictionary<string, string>?> HandleAsync(ActionHandlerContext context, IDictionary<string, string> parameters, CancellationToken cancellationToken = default)
        {
            var updates = new ConcurrentDictionary<string, string> { ["dep"] = _dep.Value };
            return Task.FromResult<IDictionary<string, string>?>(updates);
        }
    }

    /// <summary>
    /// Tests that AddWorkflowActionHandler generic method correctly registers a scoped handler factory and the executor creates a service scope to resolve scoped dependencies.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The AddWorkflowActionHandler extension method's ability to register handler types as scoped services and the executor's scoping behavior when resolving them.</para>
    /// <para><strong>Data involved:</strong> A HandlerWithDep handler class that depends on Dep (registered as scoped). The handler is registered using AddWorkflowActionHandler&lt;HandlerWithDep&gt;(), which should create a factory that resolves the handler from the service provider. The handler stores the dependency's value in workflow variables.</para>
    /// <para><strong>Why the data matters:</strong> The generic AddWorkflowActionHandler method provides a convenient way to register handler classes (rather than inline functions) while ensuring they can resolve scoped dependencies. The executor must create service scopes to properly resolve scoped handlers and their dependencies. This tests the complete registration-to-execution flow for typed handlers.</para>
    /// <para><strong>Expected outcome:</strong> ExecuteAsync should return true, and the workflow variable "dep" should be set to "dep-value", confirming the handler resolved its scoped dependency and executed successfully.</para>
    /// <para><strong>Reason for expectation:</strong> AddWorkflowActionHandler should register a factory that resolves HandlerWithDep from the service provider. When the executor executes the action, it should create a scope, resolve HandlerWithDep (which resolves Dep), and execute the handler. The workflow variable being set to "dep-value" confirms the dependency was resolved and the handler executed correctly. This validates that the generic registration method works correctly with scoped dependencies.</para>
    /// </remarks>
    [Fact]
    public async Task AddWorkflowActionHandlerTHandlerRegistersScopedHandlerFactoryAndExecutorCreatesScope()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<NotesDbContext>(o => o.UseSqlite(connection));

        services.AddScoped<Dep>();
        services.AddWorkflowActionHandler<HandlerWithDep>();

        services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<WorkflowActionHandlerRegistrar>();
        services.AddLogging();
        services.AddSingleton<IMediatorSender, ServiceProviderMediatorSender>();
        services.AddTransient<IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>, WorkflowActionRequestHandler>();
        services.AddSingleton<IWorkflowActionExecutor, WorkflowActionExecutor>();
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<WorkflowActionHandlerRegistrar>();

        // Build simple workflow def that triggers HandlerWithDep
        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w2", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A", "A", "{\"action\":\"HandlerWithDep\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        var executor = sp.GetRequiredService<IWorkflowActionExecutor>();
        var ok = await executor.ExecuteAsync("w2", def.Nodes[0], def, CancellationToken.None).ConfigureAwait(true);
        Assert.True(ok);

        var im = sp.GetRequiredService<IWorkflowInstanceManager>();
        var vars = im.GetVariables("w2");
        Assert.NotNull(vars);
        Assert.Equal("dep-value", vars["dep"]);
    }
}
