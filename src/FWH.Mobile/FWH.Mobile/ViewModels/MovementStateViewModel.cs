using CommunityToolkit.Mvvm.ComponentModel;
using FWH.Common.Location;
using FWH.Mobile.Configuration;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for displaying real-time movement state information.
/// Shows current movement state, speed, and tracking status.
/// </summary>
public partial class MovementStateViewModel : ObservableObject
{
    private readonly ILocationTrackingService _locationTrackingService;
    private readonly ILocationService _locationService;
    private readonly LocationSettings _locationSettings;
    private readonly ILogger<MovementStateViewModel> _logger;

    [ObservableProperty]
    private string _movementStateText = "Unknown";

    [ObservableProperty]
    private string _movementStateColor = "#808080"; // Gray for Unknown

    [ObservableProperty]
    private string _speedText = "--";

    [ObservableProperty]
    private string _trackingStatusText = "Tracking: Stopped";

    [ObservableProperty]
    private string _trackingStatusColor = "#DC3545"; // Red for stopped

    [ObservableProperty]
    private double? _latitude;

    [ObservableProperty]
    private double? _longitude;

    [ObservableProperty]
    private string _coordinatesText = "--";

    [ObservableProperty]
    private string _currentAddress = "--";

    [ObservableProperty]
    private bool _hasAddress = false;

    [ObservableProperty]
    private string? _businessName;

    /// <summary>
    /// Gets the display address - shows business name if available, otherwise shows address.
    /// </summary>
    public string DisplayAddress => !string.IsNullOrEmpty(BusinessName) ? BusinessName : CurrentAddress;

    public MovementStateViewModel(
        ILocationTrackingService locationTrackingService,
        ILocationService locationService,
        LocationSettings locationSettings,
        ILogger<MovementStateViewModel> logger)
    {
        _locationTrackingService = locationTrackingService ?? throw new ArgumentNullException(nameof(locationTrackingService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _locationSettings = locationSettings ?? throw new ArgumentNullException(nameof(locationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to events
        _locationTrackingService.MovementStateChanged += OnMovementStateChanged;
        _locationTrackingService.LocationUpdated += OnLocationUpdated;
        _locationTrackingService.NewLocationAddress += OnNewLocationAddress;

        // Initialize with current state
        UpdateMovementState(_locationTrackingService.CurrentMovementState);
        UpdateSpeed();
        UpdateTrackingStatus();
        UpdateCoordinates();
        UpdateAddress();
    }

    private void OnMovementStateChanged(object? sender, MovementStateChangedEventArgs e)
    {
        _logger.LogDebug("Movement state changed from {Previous} to {Current}", e.PreviousState, e.CurrentState);
        UpdateMovementState(e.CurrentState);
        UpdateSpeed();
    }

    private void OnLocationUpdated(object? sender, Common.Location.Models.GpsCoordinates e)
    {
        _logger.LogDebug("Location updated: ({Lat}, {Lon})", e.Latitude, e.Longitude);
        Latitude = e.Latitude;
        Longitude = e.Longitude;
        UpdateCoordinates();
        UpdateSpeed();
        UpdateTrackingStatus();
    }

    private void OnNewLocationAddress(object? sender, LocationAddressChangedEventArgs e)
    {
        _logger.LogInformation("Address changed to: {Address}", e.CurrentAddress);
        CurrentAddress = e.CurrentAddress;
        HasAddress = !string.IsNullOrEmpty(e.CurrentAddress);
    }

    private void UpdateAddress()
    {
        var address = _locationTrackingService.CurrentAddress;
        if (!string.IsNullOrEmpty(address))
        {
            CurrentAddress = address;
            HasAddress = true;
        }
        else
        {
            CurrentAddress = "--";
            HasAddress = false;
        }
        OnPropertyChanged(nameof(DisplayAddress));
        
        // Check for business if we have coordinates
        if (Latitude.HasValue && Longitude.HasValue)
        {
            var location = new Common.Location.Models.GpsCoordinates
            {
                Latitude = Latitude.Value,
                Longitude = Longitude.Value
            };
            _ = CheckForBusinessAsync(location);
        }
    }

    private async Task CheckForBusinessAsync(Common.Location.Models.GpsCoordinates location)
    {
        try
        {
            var business = await _locationService.GetClosestBusinessAsync(
                location.Latitude,
                location.Longitude,
                maxDistanceMeters: 100, // Check within 100m for businesses
                cancellationToken: default);

            if (business != null)
            {
                BusinessName = business.Name;
                _logger.LogDebug("Found business at current location: {BusinessName}", business.Name);
                OnPropertyChanged(nameof(DisplayAddress));
            }
            else
            {
                BusinessName = null;
                OnPropertyChanged(nameof(DisplayAddress));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for business at current location");
            BusinessName = null;
            OnPropertyChanged(nameof(DisplayAddress));
        }
    }

    private void UpdateMovementState(MovementState state)
    {
        MovementStateText = $"{state}";

        MovementStateColor = state switch
        {
            MovementState.Unknown => "#6C757D", // Gray
            MovementState.Stationary => "#007BFF", // Blue
            MovementState.Walking => "#28A745", // Green
            MovementState.Riding => "#FFC107", // Yellow/Amber
            MovementState.Moving => "#17A2B8", // Cyan
            _ => "#6C757D"
        };
    }

    private void UpdateSpeed()
    {
        var speedMph = _locationTrackingService.CurrentSpeedMph;
        var speedKmh = _locationTrackingService.CurrentSpeedKmh;

        if (!speedMph.HasValue || !speedKmh.HasValue)
        {
            SpeedText = "--";
            return;
        }

        // Display speed based on configured unit preference
        if (_locationSettings.UseKmh)
        {
            SpeedText = $"{speedKmh.Value:F1} km/h";
        }
        else
        {
            // Default to mph
            SpeedText = $"{speedMph.Value:F1} mph";
        }
    }

    private void UpdateTrackingStatus()
    {
        if (_locationTrackingService.IsTracking)
        {
            TrackingStatusText = "Tracking: Active";
            TrackingStatusColor = "#28A745"; // Green
        }
        else
        {
            TrackingStatusText = "Tracking: Stopped";
            TrackingStatusColor = "#DC3545"; // Red
        }
    }

    private void UpdateCoordinates()
    {
        if (Latitude.HasValue && Longitude.HasValue)
        {
            CoordinatesText = $"{Latitude.Value:F6}, {Longitude.Value:F6}";
        }
        else if (_locationTrackingService.LastKnownLocation != null)
        {
            var loc = _locationTrackingService.LastKnownLocation;
            Latitude = loc.Latitude;
            Longitude = loc.Longitude;
            CoordinatesText = $"{loc.Latitude:F6}, {loc.Longitude:F6}";
        }
        else
        {
            CoordinatesText = "--";
        }
    }

    /// <summary>
    /// Refreshes all displayed data from the tracking service.
    /// </summary>
    public void Refresh()
    {
        UpdateMovementState(_locationTrackingService.CurrentMovementState);
        UpdateSpeed();
        UpdateTrackingStatus();
        UpdateCoordinates();
        UpdateAddress();
    }
}
