using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FWH.Mobile.Views;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for MainView that handles navigation between different views.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _currentViewName = "Chat";

    public MainViewModel()
    {
        // Start with Chat view
        NavigateToChat();
    }

    [RelayCommand]
    private void NavigateToChat()
    {
        CurrentView = new ChatView();
        CurrentViewName = "Chat";
    }

    [RelayCommand]
    private void NavigateToMap()
    {
        CurrentView = new MapView();
        CurrentViewName = "Map";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = new SettingsView();
        CurrentViewName = "Settings";
    }

    [RelayCommand]
    private void NavigateToLog()
    {
        CurrentView = new LogView();
        CurrentViewName = "Log";
    }

    [RelayCommand]
    private void NavigateToPlaces()
    {
        CurrentView = new PlacesView();
        CurrentViewName = "Places";
    }
}
