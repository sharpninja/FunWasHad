using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile;

public partial class ChatListControl : UserControl
{
    public ChatListControl()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<FWH.Common.Chat.ViewModels.ChatListViewModel>();
    }
}
