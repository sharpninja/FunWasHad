using Avalonia.Controls;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Views;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            var logViewer = this.FindControl<LogViewerControl>("LogViewer");
            if (logViewer != null)
            {
                logViewer.DataContext = App.ServiceProvider.GetRequiredService<LogViewerViewModel>();
            }
        };
    }
}
