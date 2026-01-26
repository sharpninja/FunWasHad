using Avalonia.Controls;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        // Set DataContext to MainViewModel
        var viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
        DataContext = viewModel;

        // Set up log view row height based on build configuration
        this.Loaded += (s, e) =>
        {
            var grid = this.FindControl<Grid>("MainGrid");
            if (grid != null && grid.RowDefinitions.Count > 1)
            {
                var logViewRow = grid.RowDefinitions[1];
                if (viewModel.ShowLogViewAlways)
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
