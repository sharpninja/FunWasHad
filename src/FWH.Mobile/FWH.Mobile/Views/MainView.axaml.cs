using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FWH.Mobile.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        // Set DataContext to MainViewModel before InitializeComponent
        ViewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();

        InitializeComponent();

        // Set up log view row height based on build configuration
        this.Loaded += (s, e) =>
        {
            var grid = this.FindControl<Grid>("MainGrid");
            if (grid != null && grid.RowDefinitions.Count > 1)
            {
                var logViewRow = grid.RowDefinitions[1];
                if (ViewModel.ShowLogViewAlways)
                {
                    logViewRow.Height = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    logViewRow.Height = new GridLength(0);
                }
            }
        };
    }

    public MainViewModel? ViewModel
    {
        get => DataContext as MainViewModel;
        set => DataContext = value;
    }
}
