using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FWH.Common.Location;
using FWH.Mobile.Configuration;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;

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
    private readonly LocationApiHeartbeatService? _heartbeatService;

    [ObservableProperty]
    private string _movementStateText = "Stationary";

    [ObservableProperty]
    private ApiAvailabilityState _apiAvailabilityState = ApiAvailabilityState.Available;

    /// <summary>
    /// Gets the background color for the movement state control based on API availability state.
    /// - Available: Default background (#FAFAFA)
    /// - Unreachable (no HTTP response): Red with 50% opacity (#80DC3545)
    /// - Error (404 or 5xx): Orange with 20% opacity (#33FFA500)
    /// </summary>
    public string BackgroundColor => ApiAvailabilityState switch
    {
        ApiAvailabilityState.Available => "#FAFAFA", // Default background
        ApiAvailabilityState.Unreachable => "#80DC3545", // Red with 50% opacity (0x80 = 128 = 50% of 255)
        ApiAvailabilityState.Error => "#33FFA500", // Orange with 20% opacity (0x33 = 51 = 20% of 255)
        _ => "#FAFAFA"
    };

    partial void OnApiAvailabilityStateChanged(ApiAvailabilityState value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
    }

    [ObservableProperty]
    private string _movementStateEmoji = "😊"; // Default emoji

    [ObservableProperty]
    private string _movementStateColor = "#007BFF"; // Blue for Stationary

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
    /// Excludes coordinate strings (which are shown separately in the Location field).
    /// </summary>
    public string DisplayAddress
    {
        get
        {
            // Show business name if available
            if (!string.IsNullOrEmpty(BusinessName))
                return BusinessName;

            // Don't show coordinates as address - coordinates are shown in the Location field
            if (IsCoordinateString(CurrentAddress))
                return "--";

            // Show actual address
            return CurrentAddress ?? "--";
        }
    }

    /// <summary>
    /// Checks if a string appears to be GPS coordinates (e.g., "40.123456, -74.123456").
    /// </summary>
    private static bool IsCoordinateString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Check if it matches the coordinate pattern: numbers, comma, optional space, optional minus, numbers
        // This matches patterns like "40.123456, -74.123456" or "40.123456,-74.123456"
        var trimmed = value.Trim();
        var parts = trimmed.Split(',');
        if (parts.Length != 2)
            return false;

        // Check if both parts can be parsed as doubles
        return double.TryParse(parts[0].Trim(), out _) && double.TryParse(parts[1].Trim(), out _);
    }

    public MovementStateViewModel(
        ILocationTrackingService locationTrackingService,
        ILocationService locationService,
        LocationSettings locationSettings,
        ILogger<MovementStateViewModel> logger,
        LocationApiHeartbeatService? heartbeatService = null)
    {
        _locationTrackingService = locationTrackingService ?? throw new ArgumentNullException(nameof(locationTrackingService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _locationSettings = locationSettings ?? throw new ArgumentNullException(nameof(locationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _heartbeatService = heartbeatService;

        // Subscribe to events
        _locationTrackingService.MovementStateChanged += OnMovementStateChanged;
        _locationTrackingService.LocationUpdated += OnLocationUpdated;
        _locationTrackingService.NewLocationAddress += OnNewLocationAddress;

        // Subscribe to heartbeat service if available
        if (_heartbeatService != null)
        {
            _heartbeatService.AvailabilityChanged += OnApiAvailabilityChanged;
            ApiAvailabilityState = _heartbeatService.AvailabilityState;
        }

        // Initialize with current state
        UpdateMovementState(_locationTrackingService.CurrentMovementState);
        UpdateSpeed();
        UpdateTrackingStatus();
        UpdateCoordinates();
        UpdateAddress();
    }

    private void OnApiAvailabilityChanged(object? sender, ApiAvailabilityState state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ApiAvailabilityState = state;
            _logger.LogDebug("Location API availability changed: {State}", state);
        });
    }

    private void OnMovementStateChanged(object? sender, MovementStateChangedEventArgs e)
    {
        _logger.LogDebug("Movement state changed from {Previous} to {Current}", e.PreviousState, e.CurrentState);
        var state = e.CurrentState;
        Dispatcher.UIThread.Post(() =>
        {
            UpdateMovementState(state);
            UpdateSpeed();
        });
    }

    private void OnLocationUpdated(object? sender, Common.Location.Models.GpsCoordinates e)
    {
        _logger.LogDebug("Location updated: ({Lat}, {Lon})", e.Latitude, e.Longitude);
        var lat = e.Latitude;
        var lon = e.Longitude;
        Dispatcher.UIThread.Post(() =>
        {
            Latitude = lat;
            Longitude = lon;
            UpdateCoordinates();
            UpdateSpeed();
            UpdateTrackingStatus();
        });
    }

    private void OnNewLocationAddress(object? sender, LocationAddressChangedEventArgs e)
    {
        _logger.LogInformation("Address changed to: {Address}", e.CurrentAddress);
        var address = e.CurrentAddress;
        Dispatcher.UIThread.Post(() =>
        {
            // Don't treat coordinates as an address - they're shown in the Location field
            if (IsCoordinateString(address))
            {
                CurrentAddress = "--";
                HasAddress = false;
                _logger.LogDebug("Address is coordinates, treating as no address available");
            }
            else
            {
                CurrentAddress = address ?? "--";
                HasAddress = !string.IsNullOrEmpty(address);
            }
            OnPropertyChanged(nameof(DisplayAddress));
        });
    }

    private void UpdateAddress()
    {
        var address = _locationTrackingService.CurrentAddress;
        
        // Don't treat coordinates as an address - they're shown in the Location field
        if (IsCoordinateString(address))
        {
            CurrentAddress = "--";
            HasAddress = false;
            _logger.LogDebug("Service returned coordinates as address, treating as no address available");
        }
        else if (!string.IsNullOrEmpty(address))
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
                cancellationToken: default).ConfigureAwait(false);

            var name = business?.Name;
            Dispatcher.UIThread.Post(() =>
            {
                BusinessName = name;
                if (name != null)
                    _logger.LogDebug("Found business at current location: {BusinessName}", name);
                OnPropertyChanged(nameof(DisplayAddress));
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for business at current location");
            Dispatcher.UIThread.Post(() =>
            {
                BusinessName = null;
                OnPropertyChanged(nameof(DisplayAddress));
            });
        }
    }

    private void UpdateMovementState(MovementState state)
    {
        MovementStateText = $"{state}";
        
        // Use same emoji icons as map
        MovementStateEmoji = GetEmojiForMovementState(state);

        MovementStateColor = state switch
        {
            MovementState.Stationary => "#007BFF", // Blue
            MovementState.Walking => "#28A745", // Green
            MovementState.Riding => "#FFC107", // Yellow/Amber
            MovementState.Moving => "#17A2B8", // Cyan
            _ => "#007BFF"
        };
    }

    /// <summary>
    /// Gets the emoji icon for the movement state (same as used in MapView).
    /// </summary>
    private static string GetEmojiForMovementState(MovementState state)
    {
        return state switch
        {
            MovementState.Stationary => "😊",
            MovementState.Walking => "🚶",
            MovementState.Riding => "🚕",
            MovementState.Moving => "🚕", // Use taxi for Moving as well
            _ => "😊"
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
