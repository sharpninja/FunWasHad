using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.ViewModels;

namespace FWH.Mobile;

public partial class ChatInputControl : UserControl
{
    public ChatInputControl()
    {
        DataContext = App.ServiceProvider.GetRequiredService<FWH.Common.Chat.ViewModels.ChatInputViewModel>();
        InitializeComponent();
    }
}
