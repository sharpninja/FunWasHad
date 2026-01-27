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

    private readonly Dictionary<string, UserControl> _cachedViews = new();

    /// <summary>
    /// Gets whether the log view should always be visible (when not in RELEASE build).
    /// </summary>
    public bool ShowLogViewAlways
    {
        get
        {
#if DEBUG || STAGING
            return false;
#else
            return false;
#endif
        }
    }

    public MainViewModel()
    {
        // Start with Chat view
        NavigateToChat();
    }

    [RelayCommand]
    private void NavigateToChat()
    {
        CurrentView = GetOrCreateView("Chat", () => new ChatView());
        CurrentViewName = "Chat";
    }

    [RelayCommand]
    private void NavigateToMap()
    {
        CurrentView = GetOrCreateView("Map", () => new MapView());
        CurrentViewName = "Map";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = GetOrCreateView("Settings", () => new SettingsView());
        CurrentViewName = "Settings";
    }

    [RelayCommand]
    private void NavigateToLog()
    {
        CurrentView = GetOrCreateView("Log", () => new LogView());
        CurrentViewName = "Log";
    }

    [RelayCommand]
    private void NavigateToPlaces()
    {
        CurrentView = GetOrCreateView("Places", () => new PlacesView());
        CurrentViewName = "Places";
    }

    private UserControl GetOrCreateView(string key, Func<UserControl> factory)
    {
        if (!_cachedViews.TryGetValue(key, out var view))
        {
            view = factory();
            _cachedViews[key] = view;
        }
        return view;
    }
}
