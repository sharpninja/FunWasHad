using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FWH.Common.Chat.Tests.TestFixtures;
using FWH.Mobile.Data.Repositories;
using FWH.Mobile.Data.Data;
using FWH.Common.Workflow;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat;
using System.Linq;

namespace FWH.Common.Chat.Tests;

public class CorrelationPropagationTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;
    public CorrelationPropagationTests(SqliteTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CorrelationId_Propagates_FromChatToRepository()
    {
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();
            services.AddSingleton<IWorkflowService, WorkflowService>();
            services.AddSingleton<ChatListViewModel>();
            services.AddSingleton<ChatInputViewModel>(sp2 => new ChatInputViewModel(sp2.GetRequiredService<ChatListViewModel>()));
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<ChatService>();
        });

        var wfSvc = sp.GetRequiredService<IWorkflowService>();
        var chatSvc = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();

        var plant = "@startuml\n[*] --> A\n:A;\nA --> B\nA --> C\n@enduml";
        var def = await wfSvc.ImportWorkflowAsync(plant, "wf_corr", "CorrTest");

        // pick a custom correlation id
        var correlationId = System.Guid.NewGuid().ToString();

        await chatSvc.RenderWorkflowStateAsync(def.Id, userId: "user-123", tenantId: "tenant-a", correlationId: correlationId);

        // after initial render we should have a choice entry
        var choiceEntry = chatList.Entries.Last() as ChoiceChatEntry;
        Assert.NotNull(choiceEntry);

        // select first choice to cause an advance and repository update
        var first = choiceEntry!.Choices[0];
        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first);

        // wait for repository update log with matching correlation id
        var log = await _fixture.LoggerProvider.WaitForEntryAsync(e => e.ScopesParsed.Any(d => d.ContainsKey("CorrelationId") && d["CorrelationId"]?.ToString() == correlationId) );
        Assert.NotNull(log);

        // ensure the workflow id scope is present as well
        Assert.Contains(log!.ScopesParsed, d => d.ContainsKey("WorkflowId") && d["WorkflowId"]?.ToString() == def.Id);
    }
}
