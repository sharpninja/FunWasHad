using Microsoft.Extensions.DependencyInjection;

namespace FWH.Common.Chat.ViewModels;

public class ChatViewModel : ViewModelBase
{
    public ChatInputViewModel ChatInput { get; }
    public ChatListViewModel ChatList { get; }

    public ChatViewModel(IServiceProvider serviceProvider)
    {
        ChatInput = serviceProvider.GetRequiredService<ChatInputViewModel>();
        ChatList = serviceProvider.GetRequiredService<ChatListViewModel>();
    }
}
