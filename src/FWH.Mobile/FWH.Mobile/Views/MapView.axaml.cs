using Avalonia.Controls;
using Avalonia.Threading;
using FWH.Mobile.Services;
using FWH.Mobile.ViewModels;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Views;

public partial class MapView : UserControl
{
    private MapViewModel? _mapViewModel;
    private Services.ILocationTrackingService? _locationTrackingService;
    private MovementState _currentMovementState = FWH.Mobile.Services.MovementState.Stationary;
    private MemoryLayer? _locationHistoryLayer;
    private MemoryLayer? _deviceLocationLayer;
    private PointFeature? _deviceLocationFeature;
    private bool _isFirstLocationUpdate = true;
    private bool _isProgrammaticViewportUpdate = false;
    private MPoint? _lastProgrammaticCenter;
    private Button? _snapToLocationButton;

    /// <summary>
    /// Gets or sets whether the map is locked (user has manually panned the map).
    /// When true, the map will not auto-center on location updates.
    /// </summary>
    public bool MapLocked { get; private set; }

    /// <summary>
    /// Default visible width in meters for initial map zoom (250 meters).
    /// </summary>
    private const double DefaultVisibleMeters = 250.0;

    /// <summary>
    /// Assumed screen width in pixels for resolution calculation.
    /// This is used to convert meters to resolution (meters per pixel).
    /// </summary>
    private const double AssumedScreenWidthPixels = 1000.0;

    /// <summary>
    /// Calculates the resolution (meters per pixel) for the default zoom level.
    /// </summary>
    private static double GetDefaultResolution()
    {
        return DefaultVisibleMeters / AssumedScreenWidthPixels;
    }

    /// <summary>
    /// Maximum number of historical locations to display on the map to avoid clutter.
    /// </summary>
    private const int MaxHistoryPoints = 100;

    public MapView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            // Cache snap-to-location button reference
            _snapToLocationButton = this.FindControl<Button>("SnapToLocationButton");

            // Get MapViewModel which contains MovementStateViewModel
            _mapViewModel = App.ServiceProvider.GetRequiredService<MapViewModel>();
            if (_mapViewModel == null)
                return;

            // Subscribe to rotation property changes
            _mapViewModel.PropertyChanged += OnMapViewModelPropertyChanged;

            // Set DataContext for MovementStateControl (control named "MovementState" in AXAML)
            var movementStateControl = this.FindControl<MovementStateControl>("MovementState");
            if (movementStateControl != null && _mapViewModel.MovementStateViewModel != null)
            {
                movementStateControl.DataContext = _mapViewModel.MovementStateViewModel;

                // Subscribe to location and movement state updates
                _locationTrackingService = App.ServiceProvider.GetRequiredService<Services.ILocationTrackingService>();
                _locationTrackingService.LocationUpdated += OnLocationUpdated;
                _locationTrackingService.MovementStateChanged += OnMovementStateChanged;

                // Get current movement state
                _currentMovementState = _locationTrackingService.CurrentMovementState;
            }

            // Initialize map
            InitializeMap();

            // Update map with current location if available
            if (_mapViewModel.MovementStateViewModel != null && 
                _mapViewModel.MovementStateViewModel.Latitude.HasValue && 
                _mapViewModel.MovementStateViewModel.Longitude.HasValue)
            {
                UpdateMapLocation();
            }

            // Initialize snap-to-location button visual based on initial lock state
            UpdateSnapButtonState();
        };
    }

    private void OnMapViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.Rotation) || e.PropertyName == nameof(MapViewModel.RotationLock))
        {
            ApplyMapRotation();
        }
    }

    private void OnLocationUpdated(object? sender, Common.Location.Models.GpsCoordinates e)
    {
        // Event is raised from location tracking loop (background thread); map updates must run on UI thread.
        Dispatcher.UIThread.Post(() => UpdateMapLocation());
    }

    private void OnMovementStateChanged(object? sender, MovementStateChangedEventArgs e)
    {
        var state = e.CurrentState;
        // Event is raised from location tracking loop (background thread); map updates must run on UI thread.
        Dispatcher.UIThread.Post(() =>
        {
            _currentMovementState = state;
            UpdateMapLocation();
        });
    }

    private void InitializeMap()
    {
        var locationMap = this.FindControl<MapControl>("LocationMap");
        if (locationMap?.Map == null)
            return;

        // Add OpenStreetMap tile layer
        locationMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Create location history layer (drawn beneath current device location)
        _locationHistoryLayer = new MemoryLayer
        {
            Name = "Location History"
        };
        locationMap.Map.Layers.Add(_locationHistoryLayer);

        // Create device location marker layer
        _deviceLocationLayer = new MemoryLayer
        {
            Name = "Device Location"
        };
        locationMap.Map.Layers.Add(_deviceLocationLayer);

        // Disable map rotation (pinch zoom still works)
        locationMap.Map.Navigator.RotationLock = true;

        // Subscribe to viewport changes to detect user panning
        locationMap.Map.Navigator.ViewportChanged += OnViewportChanged;

        // Set initial map view with default zoom of 250 meters
        var defaultResolution = GetDefaultResolution();

        _isProgrammaticViewportUpdate = true;
        locationMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(0, 0), defaultResolution);
        _lastProgrammaticCenter = new MPoint(0, 0);
        _isProgrammaticViewportUpdate = false;

        // Subscribe to location updates if view model is available
        if (_mapViewModel?.MovementStateViewModel != null)
        {
            UpdateMapLocation();
        }

        // Load and render location history points
        _ = LoadLocationHistoryAsync();
    }

    private async Task LoadLocationHistoryAsync()
    {
        try
        {
            var dbContext = App.ServiceProvider.GetService<NotesDbContext>();
            if (dbContext == null || _locationHistoryLayer == null)
                return;

            // Load most recent stationary places (locations where the user actually stopped),
            // which are the ones associated with businesses and themes/logos.
            var places = await dbContext.StationaryPlaces
                .OrderByDescending(p => p.StationaryAt)
                .Take(MaxHistoryPoints)
                .ToListAsync()
                .ConfigureAwait(false);

            // Render in chronological order so older points are drawn first
            var ordered = places.OrderBy(p => p.StationaryAt).ToList();
            var features = new List<IFeature>(ordered.Count);

            foreach (var place in ordered)
            {
                // Convert WGS84 (lat/lon) to Web Mercator
                var (x, y) = SphericalMercator.FromLonLat(place.Longitude, place.Latitude);
                var point = new MPoint(x, y);
                var feature = new PointFeature(point);

                // For now, render a labeled marker for the business/location.
                // (Logos are available via StationaryPlaceEntity.LogoUrl and can be wired into
                //  Mapsui's bitmap system in a follow-up step.)
                var labelText = !string.IsNullOrWhiteSpace(place.BusinessName)
                    ? $"{place.BusinessName} {place.StationaryAt.ToLocalTime():HH:mm}"
                    : $"📍 {place.StationaryAt.ToLocalTime():HH:mm}";

                var labelStyle = new LabelStyle
                {
                    Text = labelText,
                    Font = new Font { Size = 12 },
                    BackColor = new Brush(Color.FromArgb(200, 255, 255, 255)),
                    Halo = new Pen(Color.FromArgb(255, 0, 0, 0), 1),
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                    Offset = new Offset { X = 0, Y = 0 },
                    ForeColor = Color.FromArgb(255, 0, 0, 0)
                };

                feature.Styles.Add(labelStyle);
                features.Add(feature);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_locationHistoryLayer != null)
                {
                    _locationHistoryLayer.Features = features;
                    var locationMap = this.FindControl<MapControl>("LocationMap");
                    locationMap?.Refresh();
                }
            });
        }
        catch
        {
            // Swallow exceptions to avoid impacting map rendering; history is a best-effort overlay.
        }
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        // Ignore viewport changes that we initiated programmatically
        if (_isProgrammaticViewportUpdate)
            return;

        var locationMap = this.FindControl<MapControl>("LocationMap");
        if (locationMap?.Map == null)
            return;

        // Get current center from Viewport (Viewport has CenterX and CenterY properties)
        var currentCenter = new MPoint(
            locationMap.Map.Navigator.Viewport.CenterX,
            locationMap.Map.Navigator.Viewport.CenterY);

        // Check if the center has changed significantly (user panned the map)
        // Use a small threshold to account for floating point precision
        if (_lastProgrammaticCenter != null)
        {
            var distance = Math.Sqrt(
                Math.Pow(currentCenter.X - _lastProgrammaticCenter.X, 2) +
                Math.Pow(currentCenter.Y - _lastProgrammaticCenter.Y, 2));

            // If the center moved more than a small threshold (e.g., 10 meters in map units),
            // consider it a user pan
            const double panThreshold = 10.0;
            if (distance > panThreshold)
            {
                // User panned the map; update lock state and button on UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    MapLocked = true;
                    UpdateSnapButtonState();
                });
            }
        }
        else
        {
            // No previous programmatic center recorded, assume user interaction
            Dispatcher.UIThread.Post(() =>
            {
                MapLocked = true;
                UpdateSnapButtonState();
            });
        }
    }

    private void ApplyMapRotation()
    {
        var locationMap = this.FindControl<MapControl>("LocationMap");
        if (locationMap?.Map == null || _mapViewModel == null)
            return;

        // Rotation is locked to prevent user gesture rotation (pinch-rotate)
        // but we can still set rotation programmatically if needed
        locationMap.Map.Navigator.RotateTo(_mapViewModel.Rotation);
        locationMap.Refresh();
    }

    private void UpdateMapLocation()
    {
        var locationMap = this.FindControl<MapControl>("LocationMap");
        if (locationMap?.Map == null || _mapViewModel?.MovementStateViewModel == null || 
            !_mapViewModel.MovementStateViewModel.Latitude.HasValue || 
            !_mapViewModel.MovementStateViewModel.Longitude.HasValue)
            return;

        var lat = _mapViewModel.MovementStateViewModel.Latitude.Value;
        var lon = _mapViewModel.MovementStateViewModel.Longitude.Value;

        // Convert WGS84 (lat/lon) to Web Mercator (used by maps)
        var (x, y) = SphericalMercator.FromLonLat(lon, lat);
        var locationPoint = new MPoint(x, y);

        // Update or create device location marker
        if (_deviceLocationLayer != null)
        {
            // Create or update marker feature
            _deviceLocationFeature = new PointFeature(locationPoint);
            
            // Get emoji based on movement state
            var emoji = GetEmojiForMovementState(_currentMovementState);
            
            // Style the marker with emoji text using LabelStyle
            var labelStyle = new LabelStyle
            {
                Text = emoji,
                Font = new Font { Size = 24 }, // Font size for emoji
                BackColor = new Brush(Color.FromArgb(200, 255, 255, 255)), // White background with transparency
                Halo = new Pen(Color.FromArgb(255, 0, 0, 0), 1), // Black halo for visibility
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                Offset = new Offset { X = 0, Y = 0 },
                ForeColor = Color.FromArgb(255, 0, 0, 0) // Black text (using ForeColor instead of TextColor)
            };
            _deviceLocationFeature.Styles.Add(labelStyle);
            
            // Set features on the layer (creates a new collection each time)
            _deviceLocationLayer.Features = new[] { _deviceLocationFeature };
        }

        // Set programmatic rotation if needed (user gesture rotation is locked)
        if (_mapViewModel != null)
        {
            locationMap.Map.Navigator.RotateTo(_mapViewModel.Rotation);
        }

        // Only center map on first location update or if map is not locked
        if (_isFirstLocationUpdate || !MapLocked)
        {
            if (_isFirstLocationUpdate)
            {
                _isFirstLocationUpdate = false;
            }

            // Center map on location, preserving current zoom level (don't reset zoom on updates)
            var currentResolution = locationMap.Map.Navigator.Viewport.Resolution;
            if (currentResolution > 0)
            {
                // Use current zoom level to preserve user's zoom setting
                _isProgrammaticViewportUpdate = true;
                locationMap.Map.Navigator.CenterOnAndZoomTo(locationPoint, currentResolution);
                _lastProgrammaticCenter = locationPoint;
                _isProgrammaticViewportUpdate = false;
            }
            else
            {
                // Fallback: set initial zoom to 250 meters if resolution not yet set
                var defaultResolution = GetDefaultResolution();
                
                _isProgrammaticViewportUpdate = true;
                locationMap.Map.Navigator.CenterOnAndZoomTo(locationPoint, defaultResolution);
                _lastProgrammaticCenter = locationPoint;
                _isProgrammaticViewportUpdate = false;
            }
        }
        // For subsequent updates, just refresh to show updated marker position
        locationMap.Refresh();
    }

    private void OnSnapToLocationClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Manually center map on current location and unlock the map
        MapLocked = false;
        UpdateSnapButtonState();
        CenterMapOnLocation();
    }

    private void UpdateSnapButtonState()
    {
        if (_snapToLocationButton == null)
            return;

        // When map is not locked, auto-centering is active so the button is "grayed out".
        // When map is locked (user panned), the button becomes active.
        var isEnabled = MapLocked;
        _snapToLocationButton.IsEnabled = isEnabled;
        _snapToLocationButton.Opacity = isEnabled ? 1.0 : 0.4;
    }

    private void CenterMapOnLocation()
    {
        var locationMap = this.FindControl<MapControl>("LocationMap");
        if (locationMap?.Map == null || _mapViewModel?.MovementStateViewModel == null || 
            !_mapViewModel.MovementStateViewModel.Latitude.HasValue || 
            !_mapViewModel.MovementStateViewModel.Longitude.HasValue)
            return;

        var lat = _mapViewModel.MovementStateViewModel.Latitude.Value;
        var lon = _mapViewModel.MovementStateViewModel.Longitude.Value;

        // Convert WGS84 (lat/lon) to Web Mercator (used by maps)
        var (x, y) = SphericalMercator.FromLonLat(lon, lat);
        var locationPoint = new MPoint(x, y);

        // Center map on location, preserving current zoom level
        var currentResolution = locationMap.Map.Navigator.Viewport.Resolution;
        if (currentResolution > 0)
        {
            // Use current zoom level to preserve user's zoom setting
            _isProgrammaticViewportUpdate = true;
            locationMap.Map.Navigator.CenterOnAndZoomTo(locationPoint, currentResolution);
            _lastProgrammaticCenter = locationPoint;
            _isProgrammaticViewportUpdate = false;
        }
        else
        {
            // Fallback: set initial zoom to 250 meters if resolution not yet set
            var defaultResolution = GetDefaultResolution();
            
            _isProgrammaticViewportUpdate = true;
            locationMap.Map.Navigator.CenterOnAndZoomTo(locationPoint, defaultResolution);
            _lastProgrammaticCenter = locationPoint;
            _isProgrammaticViewportUpdate = false;
        }
        locationMap.Refresh();
    }

    private static string GetEmojiForMovementState(FWH.Mobile.Services.MovementState state)
    {
        return state switch
        {
            FWH.Mobile.Services.MovementState.Stationary => "😊",
            FWH.Mobile.Services.MovementState.Walking => "🚶",
            FWH.Mobile.Services.MovementState.Riding => "🚕",
            FWH.Mobile.Services.MovementState.Moving => "🚕", // Use taxi for Moving as well
            _ => "😊"
        };
    }

}
