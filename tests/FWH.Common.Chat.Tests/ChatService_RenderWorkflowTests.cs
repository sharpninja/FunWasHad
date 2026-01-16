using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Models;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat;
using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;
using System.Collections.Generic;

namespace FWH.Common.Chat.Tests;

class TestWorkflowController : IWorkflowController
{
    private readonly Queue<WorkflowStatePayload> _responses = new();
    public void EnqueueResponse(WorkflowStatePayload p) => _responses.Enqueue(p);
    public Task<WorkflowDefinition> ImportWorkflowAsync(string plantUmlText, string? id = null, string? name = null) => Task.FromResult<WorkflowDefinition>(null!);
    public Task StartInstanceAsync(string workflowId) => Task.CompletedTask;
    public Task RestartInstanceAsync(string workflowId) => Task.CompletedTask;
    public Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId) => Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : new WorkflowStatePayload(false, "", System.Array.Empty<WorkflowChoiceOption>()));
    public Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue) => Task.FromResult(true);
    public string? GetCurrentNodeId(string workflowId) => null;
    public bool WorkflowExists(string workflowId) => true;
}

public class ChatServiceRenderTests
{
    [Fact]
    public async Task RenderWorkflowStateAsync_AppendsTextEntryWhenNotChoice()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ChatListViewModel>();
        services.AddSingleton<ChatInputViewModel>(sp => new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
        services.AddSingleton<ChatViewModel>();

        var controller = new TestWorkflowController();
        controller.EnqueueResponse(new WorkflowStatePayload(false, "Hello from node", System.Array.Empty<WorkflowChoiceOption>()));
        services.AddSingleton<IWorkflowController>(controller);
        services.AddSingleton<IWorkflowService, WorkflowService>();

        // Register SRP components
        services.AddSingleton<IWorkflowToChatConverter, WorkflowToChatConverter>();
        services.AddSingleton<IChatDuplicateDetector, ChatDuplicateDetector>();

        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ChatService>>();
        var converter = sp.GetRequiredService<IWorkflowToChatConverter>();
        var detector = sp.GetRequiredService<IChatDuplicateDetector>();
        var chatSvc = new ChatService(sp, converter, detector, logger);
        var chatList = sp.GetRequiredService<ChatListViewModel>();

        await chatSvc.RenderWorkflowStateAsync("wf-x");

        Assert.Single(chatList.Entries);
        var entry = chatList.Entries[0];
        Assert.Equal(FWH.Common.Chat.ViewModels.ChatAuthors.Bot, entry.Author);
    }

    [Fact]
    public async Task RenderWorkflowStateAsync_AppendsChoiceAndAdvancesOnSelection()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ChatListViewModel>();
        services.AddSingleton<ChatInputViewModel>(sp => new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
        services.AddSingleton<ChatViewModel>();

        var controller = new TestWorkflowController();

        var options = new WorkflowChoiceOption[] {
            new WorkflowChoiceOption(0, "To B", "B"),
            new WorkflowChoiceOption(1, "To C", "C")
        };

        var payload = new WorkflowStatePayload(true, "Choose:", options);
        var next = new WorkflowStatePayload(false, "Arrived at B", System.Array.Empty<WorkflowChoiceOption>());
        controller.EnqueueResponse(payload);
        controller.EnqueueResponse(next);

        services.AddSingleton<IWorkflowController>(controller);
        services.AddSingleton<IWorkflowService, WorkflowService>();

        // Register SRP components
        services.AddSingleton<IWorkflowToChatConverter, WorkflowToChatConverter>();
        services.AddSingleton<IChatDuplicateDetector, ChatDuplicateDetector>();

        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ChatService>>();
        var converter = sp.GetRequiredService<IWorkflowToChatConverter>();
        var detector = sp.GetRequiredService<IChatDuplicateDetector>();
        var chatSvc = new ChatService(sp, converter, detector, logger);
        var chatList = sp.GetRequiredService<ChatListViewModel>();

        await chatSvc.RenderWorkflowStateAsync("wf-x");

        // Chat list should contain the choice entry
        Assert.Single(chatList.Entries);
        var choiceEntry = chatList.Entries[0] as FWH.Common.Chat.ViewModels.ChoiceChatEntry;
        Assert.NotNull(choiceEntry);
        Assert.Equal(2, choiceEntry!.Choices.Count);

        // Simulate selecting the first choice by invoking the SelectChoiceCommand
        var first = choiceEntry.Choices[0];
        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first);

        // After selection and automatic render, chat list should now have two entries (choice + resulting text)
        Assert.Equal(2, chatList.Entries.Count);
        var textEntry = chatList.Entries[1] as FWH.Common.Chat.ViewModels.TextChatEntry;
        Assert.NotNull(textEntry);
        Assert.Equal("Arrived at B", textEntry!.Text);
    }
}
