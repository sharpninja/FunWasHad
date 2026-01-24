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

        // Set DataContext to MainViewModel
        DataContext = App.ServiceProvider.GetRequiredService<MainViewModel>();
    }

    public MainViewModel? ViewModel
    {
        get => DataContext as MainViewModel;
        set => DataContext = value;
    }
}
