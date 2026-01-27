using Avalonia.Controls;
using FWH.Mobile.Services;
using FWH.Mobile.ViewModels;
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
    private MovementState _currentMovementState = FWH.Mobile.Services.MovementState.Unknown;
    private MemoryLayer? _deviceLocationLayer;
    private PointFeature? _deviceLocationFeature;

    public MapView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
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
        UpdateMapLocation();
    }

    private void OnMovementStateChanged(object? sender, MovementStateChangedEventArgs e)
    {
        _currentMovementState = e.CurrentState;
        UpdateMapLocation();
    }

    private void InitializeMap()
    {
        var locationMap = this.FindControl<MapControl>("LocationMap");
        if (locationMap?.Map == null)
            return;

        // Add OpenStreetMap tile layer
        locationMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Create device location marker layer
        _deviceLocationLayer = new MemoryLayer
        {
            Name = "Device Location"
        };
        locationMap.Map.Layers.Add(_deviceLocationLayer);

        // Disable map rotation (pinch zoom still works)
        locationMap.Map.Navigator.RotationLock = true;

        // Set initial map view (Mapsui 5: use Navigator.CenterOnAndZoomTo instead of Home)
        var resolutions = locationMap.Map.Navigator.Resolutions;
        if (resolutions.Count > 9)
            locationMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(0, 0), resolutions[9]);

        // Subscribe to location updates if view model is available
        if (_mapViewModel?.MovementStateViewModel != null)
        {
            UpdateMapLocation();
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

        // Center map on location (Mapsui 5: use Navigator.CenterOnAndZoomTo)
        var resolutions = locationMap.Map.Navigator.Resolutions;
        if (resolutions.Count > 15)
            locationMap.Map.Navigator.CenterOnAndZoomTo(locationPoint, resolutions[15]);
        locationMap.Refresh();
    }

    private static string GetEmojiForMovementState(FWH.Mobile.Services.MovementState state)
    {
        return state switch
        {
            FWH.Mobile.Services.MovementState.Unknown => "😊",
            FWH.Mobile.Services.MovementState.Stationary => "😊",
            FWH.Mobile.Services.MovementState.Walking => "🚶",
            FWH.Mobile.Services.MovementState.Riding => "🚕",
            FWH.Mobile.Services.MovementState.Moving => "🚕", // Use taxi for Moving as well
            _ => "😊" // Default to smiley
        };
    }
}
