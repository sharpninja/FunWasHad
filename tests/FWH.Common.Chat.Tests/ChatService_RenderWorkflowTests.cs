using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Chat.Tests;

public class ChatServiceRenderTests
{
    /// <summary>
    /// Tests that RenderWorkflowStateAsync appends a TextChatEntry to the chat list when the workflow state is not a choice (IsChoice=false).
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The ChatService.RenderWorkflowStateAsync method's behavior when the workflow state payload represents a text message rather than a choice.</para>
    /// <para><strong>Data involved:</strong> A TestWorkflowController that returns a WorkflowStatePayload with IsChoice=false and Text="Hello from node". The ChatService renders this state to the chat list. The test uses a mock controller to control the workflow state response.</para>
    /// <para><strong>Why the data matters:</strong> Workflows can output text messages (e.g., informational messages, prompts) that should be displayed as bot messages in the chat. When IsChoice=false, the state represents a simple text message, not a choice point. The ChatService must correctly convert this to a TextChatEntry and append it to the chat list so users can see the workflow's output.</para>
    /// <para><strong>Expected outcome:</strong> After RenderWorkflowStateAsync completes, the chat list should contain exactly one entry with Author=Bot, representing the text message from the workflow.</para>
    /// <para><strong>Reason for expectation:</strong> The WorkflowToChatConverter should detect that IsChoice=false and create a TextChatEntry with the payload's Text content. The entry should have Author=Bot (since it's from the workflow, not the user). The single entry confirms that the text was correctly converted and added to the chat list, enabling users to see workflow messages.</para>
    /// </remarks>
    [Fact]
    public async Task RenderWorkflowStateAsyncAppendsTextEntryWhenNotChoice()
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

        await chatSvc.RenderWorkflowStateAsync("wf-x").ConfigureAwait(true);

        Assert.Single(chatList.Entries);
        var entry = chatList.Entries[0];
        Assert.Equal(FWH.Common.Chat.ViewModels.ChatAuthors.Bot, entry.Author);
    }

    /// <summary>
    /// Tests that RenderWorkflowStateAsync appends a ChoiceChatEntry when the workflow state is a choice (IsChoice=true), and that selecting a choice automatically advances the workflow and renders the next state.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The ChatService.RenderWorkflowStateAsync method's behavior when the workflow state is a choice, and the automatic workflow advancement and re-rendering that occurs when a user selects a choice.</para>
    /// <para><strong>Data involved:</strong> A TestWorkflowController that returns two WorkflowStatePayload responses: first a choice payload (IsChoice=true, Text="Choose:", with two options "To B" and "To C"), then a text payload (IsChoice=false, Text="Arrived at B"). The ChatService renders the choice, then the test simulates selecting the first choice, which should trigger automatic advancement and re-rendering.</para>
    /// <para><strong>Why the data matters:</strong> Workflows often present choices to users (e.g., "Yes/No", "Option A/B/C"). When a choice is rendered, it should appear as a ChoiceChatEntry with selectable options. When a user selects a choice, the workflow should automatically advance to the next node and render the resulting state. This test validates the complete flow: choice rendering → user selection → automatic advancement → next state rendering.</para>
    /// <para><strong>Expected outcome:</strong> After initial render, the chat list should contain one ChoiceChatEntry with 2 choices. After selecting the first choice, the chat list should contain 2 entries (the choice entry and a new TextChatEntry with text "Arrived at B"), confirming that the workflow advanced and rendered the next state.</para>
    /// <para><strong>Reason for expectation:</strong> The WorkflowToChatConverter should detect IsChoice=true and create a ChoiceChatEntry with the provided options. When SelectChoiceCommand is executed, it should call AdvanceByChoiceValueAsync on the controller, which advances the workflow. The ChatService should then automatically call RenderWorkflowStateAsync again to render the new state, which should be the text payload "Arrived at B". The presence of both entries confirms the complete choice selection and advancement flow works correctly.</para>
    /// </remarks>
    [Fact]
    public async Task RenderWorkflowStateAsyncAppendsChoiceAndAdvancesOnSelection()
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

        await chatSvc.RenderWorkflowStateAsync("wf-x").ConfigureAwait(true);

        // Chat list should contain the choice entry
        Assert.Single(chatList.Entries);
        var choiceEntry = chatList.Entries[0] as FWH.Common.Chat.ViewModels.ChoiceChatEntry;
        Assert.NotNull(choiceEntry);
        Assert.Equal(2, choiceEntry!.Choices.Count);

        // Simulate selecting the first choice by invoking the SelectChoiceCommand
        var first = choiceEntry.Choices[0];
        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first).ConfigureAwait(true);

        // After selection and automatic render, chat list should now have two entries (choice + resulting text)
        Assert.Equal(2, chatList.Entries.Count);
        var textEntry = chatList.Entries[1] as FWH.Common.Chat.ViewModels.TextChatEntry;
        Assert.NotNull(textEntry);
        Assert.Equal("Arrived at B", textEntry!.Text);
    }
}
