using Avalonia.Controls;
using FWH.Mobile.ViewModels;
using FWH.Common.Chat.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        
        Loaded += (_, _) =>
        {
            var logViewer = this.FindControl<LogViewerControl>("LogViewer");
            if (logViewer != null)
            {
                logViewer.DataContext = App.ServiceProvider.GetRequiredService<LogViewerViewModel>();
            }

            // Set DataContext for MovementStateControl
            var movementStateControl = this.FindControl<MovementStateControl>("MovementState");
            if (movementStateControl != null)
            {
                movementStateControl.DataContext = App.ServiceProvider.GetRequiredService<MovementStateViewModel>();
            }
        };
    }

    public FWH.Common.Chat.ViewModels.ChatViewModel? ViewModel
    {
        get => DataContext as FWH.Common.Chat.ViewModels.ChatViewModel;
        set => DataContext = value;
    }
}
