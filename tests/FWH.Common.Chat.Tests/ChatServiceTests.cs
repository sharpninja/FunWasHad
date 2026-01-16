using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FWH.Common.Chat;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;
using CommunityToolkit.Mvvm.Input;

namespace FWH.Common.Chat.Tests;

public class ChatServiceTests
{
    private ServiceProvider BuildServices()
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

    [Fact]
    public async Task StartAsync_PopulatesInitialEntries()
    {
        var sp = BuildServices();
        var service = sp.GetRequiredService<ChatService>();
        var vm = sp.GetRequiredService<ChatViewModel>();

        await service.StartAsync();

        var list = vm.ChatList;
        Assert.NotEmpty(list.Entries);

        // First entry should be a TextChatEntry from Bot
        var first = list.Entries.First();
        Assert.Equal(FWH.Common.Chat.ViewModels.ChatAuthors.Bot, first.Author);
    }

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

        await Task.WhenAll(cmd1.ExecuteAsync(item1), cmd2.ExecuteAsync(item2));

        Assert.Equal(2, invoked);
    }

    [Fact]
    public void ChoicePayload_AddChoice_HandlesDuplicateIndex()
    {
        var payload = new ChoicePayload(new[] { new ChoicesItem(0, "First", 1) });
        var added = payload.AddChoice("Second", 2, 0);

        // Should still have two items
        Assert.Equal(2, payload.Choices.Count);
        // The newly added item should be present
        Assert.Contains(payload.Choices, c => c.ChoiceText == "Second");
    }
}
