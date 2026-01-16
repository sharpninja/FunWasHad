using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.Tests.TestFixtures;
using FWH.Mobile.Data.Repositories;
using FWH.Mobile.Data.Data;
using FWH.Common.Workflow;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat;

namespace FWH.Common.Chat.Tests;

public class ScopeUserTenantTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;
    public ScopeUserTenantTests(SqliteTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UserIdAndTenantId_AppearInScopes_EndToEnd()
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
        var def = await wfSvc.ImportWorkflowAsync(plant, "wf_scope", "ScopeTest");

        var userId = "user-7";
        var tenantId = "tenant-x";

        await chatSvc.RenderWorkflowStateAsync(def.Id, userId: userId, tenantId: tenantId);

        // after initial render we should have a choice entry
        var choiceEntry = chatList.Entries.Last() as ChoiceChatEntry;
        Assert.NotNull(choiceEntry);

        // select first to trigger advance and repository update
        var first = choiceEntry!.Choices[0];
        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first);

        var log = await _fixture.LoggerProvider.WaitForEntryAsync(e =>
            e.ScopesParsed.Any(d => d.ContainsKey("UserId") && d["UserId"]?.ToString() == userId)
            && e.ScopesParsed.Any(d => d.ContainsKey("TenantId") && d["TenantId"]?.ToString() == tenantId)
        );

        Assert.NotNull(log);
        Assert.Contains(log!.ScopesParsed, d => d.ContainsKey("WorkflowId") && d["WorkflowId"]?.ToString() == def.Id);
    }
}
