using Avalonia.Controls;
using FWH.Mobile.ViewModels;
using FWH.Mobile.Services;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FWH.Mobile.Views;

public partial class MapView : UserControl
{
    private MovementStateViewModel? _viewModel;
    private Services.ILocationTrackingService? _locationTrackingService;
    private MovementState _currentMovementState = FWH.Mobile.Services.MovementState.Unknown;

    public MapView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            // Set DataContext for MovementStateControl (control named "MovementState" in AXAML)
            var movementStateControl = this.FindControl<MovementStateControl>("MovementState");
            if (movementStateControl != null)
            {
                _viewModel = App.ServiceProvider.GetRequiredService<MovementStateViewModel>();
                movementStateControl.DataContext = _viewModel;

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
            if (_viewModel != null && _viewModel.Latitude.HasValue && _viewModel.Longitude.HasValue)
            {
                UpdateMapLocation();
            }
        };
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

        // Set initial map view (Mapsui 5: use Navigator.CenterOnAndZoomTo instead of Home)
        var resolutions = locationMap.Map.Navigator.Resolutions;
        if (resolutions.Count > 9)
            locationMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(0, 0), resolutions[9]);

        // Subscribe to location updates if view model is available
        if (_viewModel != null)
        {
            UpdateMapLocation();
        }
    }

    private void UpdateMapLocation()
    {
        var locationMap = this.FindControl<MapControl>("LocationMap");
        if (locationMap?.Map == null || _viewModel == null || !_viewModel.Latitude.HasValue || !_viewModel.Longitude.HasValue)
            return;

        var lat = _viewModel.Latitude.Value;
        var lon = _viewModel.Longitude.Value;

        // Convert WGS84 (lat/lon) to Web Mercator (used by maps)
        var (x, y) = SphericalMercator.FromLonLat(lon, lat);

        // Center map on location (Mapsui 5: use Navigator.CenterOnAndZoomTo)
        // TODO: Restore device marker layer when Mapsui 5 MemoryLayer API is confirmed
        var resolutions = locationMap.Map.Navigator.Resolutions;
        if (resolutions.Count > 15)
            locationMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), resolutions[15]);
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
