using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FWH.Mobile.Configuration;
using FWH.Mobile.Services;
using System;
using System.Threading.Tasks;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for displaying activity tracking information.
/// </summary>
public partial class ActivityTrackingViewModel : ObservableObject
{
    private readonly ActivityTrackingService _activityTrackingService;
    private readonly ILocationTrackingService _locationTrackingService;
    private readonly LocationSettings _locationSettings;

    [ObservableProperty]
    private string _currentState = "Unknown";

    [ObservableProperty]
    private string _currentSpeed = "0.0 mph";

    [ObservableProperty]
    private string _activitySummary = "No active activity";

    [ObservableProperty]
    private bool _isTracking;

    public ActivityTrackingViewModel(
        ActivityTrackingService activityTrackingService,
        ILocationTrackingService locationTrackingService)
    {
        _activityTrackingService = activityTrackingService ?? throw new ArgumentNullException(nameof(activityTrackingService));
        _locationTrackingService = locationTrackingService ?? throw new ArgumentNullException(nameof(locationTrackingService));

        // Subscribe to state changes
        _locationTrackingService.MovementStateChanged += OnMovementStateChanged;

        // Start monitoring
        _activityTrackingService.StartMonitoring();

        // Update initial state
        UpdateDisplay();
    }

    private void OnMovementStateChanged(object? sender, MovementStateChangedEventArgs e)
    {
        // Update display on state change
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        CurrentState = _locationTrackingService.CurrentMovementState.ToString();
        
        // Display speed based on configured unit preference
        if (_locationSettings.UseKmh)
        {
            var speed = _locationTrackingService.CurrentSpeedKmh;
            CurrentSpeed = speed.HasValue ? $"{speed:F1} km/h" : "0.0 km/h";
        }
        else
        {
            // Default to mph
            var speed = _locationTrackingService.CurrentSpeedMph;
            CurrentSpeed = speed.HasValue ? $"{speed:F1} mph" : "0.0 mph";
        }

        ActivitySummary = _activityTrackingService.GetActivitySummary();
        IsTracking = _activityTrackingService.IsTrackingActivity;
    }

    [RelayCommand]
    private void RefreshDisplay()
    {
        UpdateDisplay();
    }

    public void Dispose()
    {
        _locationTrackingService.MovementStateChanged -= OnMovementStateChanged;
        _activityTrackingService.StopMonitoring();
    }
}
