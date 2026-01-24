using Avalonia.Controls;
using FWH.Common.Chat.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            // Set DataContext for ChatViewModel
            DataContext = App.ServiceProvider.GetRequiredService<ChatViewModel>();
        };
    }
}
