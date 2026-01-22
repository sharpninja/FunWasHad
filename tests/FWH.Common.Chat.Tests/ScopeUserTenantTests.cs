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

    /// <summary>
    /// Tests that UserId and TenantId are correctly propagated through logging scopes in the end-to-end workflow execution flow.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The logging scope propagation mechanism's ability to carry UserId and TenantId through the workflow execution pipeline from ChatService through to repository operations.</para>
    /// <para><strong>Data involved:</strong> A workflow with branching node A, userId="user-7", tenantId="tenant-x". The workflow is rendered, a choice is selected, which triggers workflow advancement and repository updates. A logger provider captures log entries with their scopes.</para>
    /// <para><strong>Why the data matters:</strong> Multi-tenant applications need to track which user and tenant initiated operations for auditing, debugging, and data isolation. Logging scopes allow this context to flow through the entire call stack without explicitly passing parameters. This test validates that UserId and TenantId are correctly propagated from the ChatService entry point through to repository operations, ensuring logs can be filtered by user/tenant.</para>
    /// <para><strong>Expected outcome:</strong> After selecting a choice and triggering workflow advancement, a log entry should be captured that contains scopes with UserId="user-7", TenantId="tenant-x", and WorkflowId matching the workflow definition ID.</para>
    /// <para><strong>Reason for expectation:</strong> The ChatService should create logging scopes with UserId and TenantId when RenderWorkflowStateAsync is called. These scopes should propagate through the workflow controller, action executor, and repository operations. The logger provider should capture log entries with these scopes intact. The presence of all three scope keys (UserId, TenantId, WorkflowId) confirms that scope propagation works correctly throughout the execution pipeline, enabling proper audit trails and multi-tenant logging.</para>
    /// </remarks>
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
