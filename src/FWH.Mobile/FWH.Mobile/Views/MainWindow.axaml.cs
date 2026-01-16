using Avalonia.Controls;
using FWH.Mobile.ViewModels;

namespace FWH.Mobile.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Wire up LogViewerControl DataContext from Tag
        Opened += (s, e) =>
        {
            if (Tag is LogViewerViewModel logViewerVm)
            {
                var logViewer = this.FindControl<LogViewerControl>("LogViewer");
                if (logViewer != null)
                    logViewer.DataContext = logViewerVm;
            }
        };
    }
}

