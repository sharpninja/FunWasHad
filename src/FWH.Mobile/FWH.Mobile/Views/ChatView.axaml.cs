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
            // Resolve ChatViewModel off UI thread to avoid ANR (ChatService/workflow graph can be heavy).
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                var vm = App.ServiceProvider.GetRequiredService<ChatViewModel>();
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (IsLoaded)
                    {
                        DataContext = vm;
                    }
                });
            });
        };
    }
}
