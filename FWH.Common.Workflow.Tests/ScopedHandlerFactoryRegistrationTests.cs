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
using FWH.Common.Workflow.Extensions;
using System.Collections.Concurrent;

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
        public Task<IDictionary<string,string>?> HandleAsync(ActionHandlerContext context, IDictionary<string,string> parameters, CancellationToken cancellationToken = default)
        {
            var updates = new ConcurrentDictionary<string,string> { ["dep"] = _dep.Value };
            return Task.FromResult<IDictionary<string,string>?>(updates);
        }
    }

    [Fact]
    public async Task AddWorkflowActionHandler_THandler_RegistersScopedHandlerFactory_AndExecutorCreatesScope()
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
        services.AddSingleton<IWorkflowActionExecutor, WorkflowActionExecutor>();
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<WorkflowActionHandlerRegistrar>();

        // Build simple workflow def that triggers HandlerWithDep
        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w2", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A","A","{\"action\":\"HandlerWithDep\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        var executor = sp.GetRequiredService<IWorkflowActionExecutor>();
        var ok = await executor.ExecuteAsync("w2", def.Nodes[0], def, CancellationToken.None);
        Assert.True(ok);

        var im = sp.GetRequiredService<IWorkflowInstanceManager>();
        var vars = im.GetVariables("w2");
        Assert.NotNull(vars);
        Assert.Equal("dep-value", vars["dep"]);
    }
}
