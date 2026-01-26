using CommunityToolkit.Mvvm.Input;
using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;
using FWH.Common.Chat.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Chat.Tests;

public class ChatServiceTests
{
    private IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ChatListViewModel>();
        services.AddSingleton<ChatInputViewModel>(sp =>
            new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
        services.AddSingleton<ChatViewModel>();

        // Register SRP components
        services.AddSingleton<IWorkflowToChatConverter, WorkflowToChatConverter>();
        services.AddSingleton<IChatDuplicateDetector, ChatDuplicateDetector>();

        services.AddSingleton<ChatService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Tests that ChatService.StartAsync populates the chat list with initial entries when the service starts.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The ChatService.StartAsync method's ability to initialize the chat interface with initial bot messages when the service starts.</para>
    /// <para><strong>Data involved:</strong> A fresh ChatService instance with a ChatViewModel containing an empty ChatListViewModel. The service is started without any prior workflow or chat history.</para>
    /// <para><strong>Why the data matters:</strong> The initial chat entries provide the first interaction point for users. These entries typically include welcome messages or initial workflow prompts from the bot. Testing with a fresh service ensures the initialization logic works correctly on first startup.</para>
    /// <para><strong>Expected outcome:</strong> After StartAsync completes, the ChatList.Entries collection should not be empty, and the first entry should have Author = Bot.</para>
    /// <para><strong>Reason for expectation:</strong> The ChatService should initialize the chat interface with at least one bot message to start the conversation. The first entry being from the Bot is expected because workflows typically begin with bot-initiated messages that prompt user interaction.</para>
    /// </remarks>
    [Fact]
    public async Task StartAsyncPopulatesInitialEntries()
    {
        var sp = BuildServices();
        var service = sp.GetRequiredService<ChatService>();
        var vm = sp.GetRequiredService<ChatViewModel>();

        await service.StartAsync().ConfigureAwait(true);

        var list = vm.ChatList;
        Assert.NotEmpty(list.Entries);

        // First entry should be a TextChatEntry from Bot
        var first = list.Entries.First();
        Assert.Equal(FWH.Common.Chat.ViewModels.ChatAuthors.Bot, first.Author);
    }

    /// <summary>
    /// Tests that adding a ChoiceChatEntry to the chat list automatically sets the choices in ChatInputViewModel.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The integration between ChatListViewModel and ChatInputViewModel when choice-based messages are added to the chat.</para>
    /// <para><strong>Data involved:</strong> A ChoicePayload containing a single ChoicesItem with index 0, text "A", and value 1. This ChoicePayload is wrapped in a ChoiceChatEntry from the Bot author and added to the chat list.</para>
    /// <para><strong>Why the data matters:</strong> Choice-based interactions are a core workflow feature - the bot presents options and the user selects one. When a ChoiceChatEntry becomes the current message, the ChatInputViewModel must be updated to show the choice UI and handle selection. This test validates the reactive binding between the chat list and input view models.</para>
    /// <para><strong>Expected outcome:</strong> After adding the ChoiceChatEntry, vm.ChatInput.Choices should not be null and should contain exactly one choice item matching the added ChoicePayload.</para>
    /// <para><strong>Reason for expectation:</strong> The ChatViewModel should observe changes to the ChatList and update ChatInput.Choices when a ChoiceChatEntry becomes the current message. This enables the UI to switch from text input mode to choice selection mode, which is required for workflow-driven interactions.</para>
    /// </remarks>
    [Fact]
    public void ChatInput_SetChoices_AttachesChoiceHandlers()
    {
        var sp = BuildServices();
        var vm = sp.GetRequiredService<ChatViewModel>();

        var choice = new ChoicePayload(new[] { new ChoicesItem(0, "A", 1) });
        var list = vm.ChatList;
        list.AddEntry(new ChoiceChatEntry(FWH.Common.Chat.ViewModels.ChatAuthors.Bot, choice));

        // Should set vm.ChatInput.Choices when current changes
        // Simulate property change by calling AddEntry above
        Assert.NotNull(vm.ChatInput.Choices);
        Assert.Single(vm.ChatInput.Choices!.Choices);
    }

    /// <summary>
    /// Tests that SendCommand can be executed with null text and invokes the TextSubmitted event with an empty string.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The SendCommand's handling of null text input and its ability to raise the TextSubmitted event.</para>
    /// <para><strong>Data involved:</strong> ChatInputViewModel with Text property set to null. An event handler is attached to TextSubmitted to capture the submitted value. The initial captured value is "not-set" to verify the event fires.</para>
    /// <para><strong>Why the data matters:</strong> Users may submit empty messages (e.g., pressing Enter without typing), and the UI may have null text values. The command must handle these edge cases gracefully by normalizing null to empty string, ensuring consistent behavior and preventing null reference exceptions in downstream handlers.</para>
    /// <para><strong>Expected outcome:</strong> After executing SendCommand, the TextSubmitted event should fire and the received value should be string.Empty (not null).</para>
    /// <para><strong>Reason for expectation:</strong> The SendCommand should normalize null text to empty string before raising the event. This provides a consistent contract for event handlers and prevents null reference exceptions. Empty strings are valid input (e.g., for clearing or confirming actions) and should be processed normally.</para>
    /// </remarks>
    [Fact]
    public void SendCommand_AllowsNullTextAndInvokesEvent()
    {
        var sp = BuildServices();
        var input = sp.GetRequiredService<ChatInputViewModel>();

        string? received = "not-set";
        input.TextSubmitted += (s, e) => received = e;

        input.Text = null;

        // Execute the generated SendCommand
        input.SendCommand.Execute(null);

        Assert.Equal(string.Empty, received);
    }

    /// <summary>
    /// Tests that SelectChoiceCommand can execute concurrently on different ChoicesItem instances without race conditions.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The thread-safety and concurrent execution capability of SelectChoiceCommand when multiple choice items are selected simultaneously.</para>
    /// <para><strong>Data involved:</strong> Two ChoicesItem instances: item1 with index 0, text "One", value 1; item2 with index 1, text "Two", value 2. Each item has a ChoiceSubmitted event handler that increments a shared counter. Both commands are executed concurrently using Task.WhenAll.</para>
    /// <para><strong>Why the data matters:</strong> In a UI scenario, users might rapidly click multiple choice buttons, or the system might process multiple choice selections concurrently. The command execution must be thread-safe to prevent race conditions, lost events, or incorrect state updates. This test validates that concurrent executions don't interfere with each other.</para>
    /// <para><strong>Expected outcome:</strong> After both commands complete, the invoked counter should equal 2, indicating both ChoiceSubmitted events fired correctly.</para>
    /// <para><strong>Reason for expectation:</strong> Each ChoicesItem instance should have its own independent command execution path. Concurrent execution should not cause events to be lost or handlers to be skipped. The counter incrementing twice confirms both events fired, validating thread-safety and proper event handling in concurrent scenarios.</para>
    /// </remarks>
    [Fact]
    public async Task SelectChoiceCommand_CanRunConcurrentlyOnDifferentItems()
    {
        var item1 = new ChoicesItem(0, "One", 1);
        var item2 = new ChoicesItem(1, "Two", 2);

        int invoked = 0;
        item1.ChoiceSubmitted += (s, e) => invoked++;
        item2.ChoiceSubmitted += (s, e) => invoked++;

        var cmd1 = (IAsyncRelayCommand)item1.SelectChoiceCommand;
        var cmd2 = (IAsyncRelayCommand)item2.SelectChoiceCommand;

        await Task.WhenAll(cmd1.ExecuteAsync(item1), cmd2.ExecuteAsync(item2)).ConfigureAwait(true);

        Assert.Equal(2, invoked);
    }

    /// <summary>
    /// Tests that ChoicePayload.AddChoice correctly handles duplicate indices by adding the new choice without removing the existing one.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The ChoicePayload.AddChoice method's behavior when adding a choice with an index that already exists in the collection.</para>
    /// <para><strong>Data involved:</strong> A ChoicePayload initialized with one ChoicesItem (index 0, text "First", value 1). A second choice is added with the same index (0) but different text "Second" and value 2.</para>
    /// <para><strong>Why the data matters:</strong> Duplicate indices can occur due to workflow definition errors or dynamic choice generation. The AddChoice method should handle this gracefully by allowing multiple choices with the same index rather than throwing an exception or overwriting existing choices. This ensures robust behavior and prevents data loss.</para>
    /// <para><strong>Expected outcome:</strong> After adding the duplicate-index choice, the Choices collection should contain exactly 2 items, and one of them should have ChoiceText = "Second".</para>
    /// <para><strong>Reason for expectation:</strong> The AddChoice method should append the new choice to the collection regardless of index conflicts. This allows the collection to grow and preserves all choices, even if they have duplicate indices. The presence of "Second" confirms the new choice was added successfully alongside the existing "First" choice.</para>
    /// </remarks>
    [Fact]
    public void ChoicePayloadAddChoiceHandlesDuplicateIndex()
    {
        var payload = new ChoicePayload(new[] { new ChoicesItem(0, "First", 1) });
        var added = payload.AddChoice("Second", 2, 0);

        // Should still have two items
        Assert.Equal(2, payload.Choices.Count);
        // The newly added item should be present
        Assert.True(payload.Choices.Any(c => c.ChoiceText == "Second"), "Expected choice 'Second' not found");
    }
}
