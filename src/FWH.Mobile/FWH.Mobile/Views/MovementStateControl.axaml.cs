using Avalonia.Controls;
using FWH.Mobile.ViewModels;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;
using System;

namespace FWH.Mobile.Views;

/// <summary>
/// UserControl that displays real-time movement state information.
/// Shows current movement state, speed, location, and tracking status.
/// Includes a native map control showing the device location.
/// </summary>
public partial class MovementStateControl : UserControl
{
    private MovementStateViewModel? _viewModel;
    private MarkerLayer? _locationMarkerLayer;

    public MovementStateControl()
    {
        InitializeComponent();
        InitializeMap();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MovementStateViewModel viewModel)
        {
            // Unsubscribe from previous view model
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Update map with initial location
            UpdateMapLocation();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MovementStateViewModel.Latitude) ||
            e.PropertyName == nameof(MovementStateViewModel.Longitude))
        {
            UpdateMapLocation();
        }
    }

    private void InitializeMap()
    {
        if (LocationMap?.Map == null)
            return;

        // Add OpenStreetMap tile layer
        LocationMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        // Create a marker layer for the device location
        _locationMarkerLayer = new MarkerLayer("DeviceLocation")
        {
            Style = null // Will be set per marker
        };
        LocationMap.Map.Layers.Add(_locationMarkerLayer);

        // Set initial view (will be updated when location is available)
        LocationMap.Map.Home = n => n.NavigateTo(new MPoint(0, 0), n.Resolutions[9]); // Zoom level 9
    }

    private void UpdateMapLocation()
    {
        if (LocationMap?.Map == null || _viewModel == null)
            return;

        if (!_viewModel.Latitude.HasValue || !_viewModel.Longitude.HasValue)
            return;

        var lat = _viewModel.Latitude.Value;
        var lon = _viewModel.Longitude.Value;

        // Convert WGS84 (lat/lon) to Web Mercator (used by maps)
        var (x, y) = SphericalMercator.FromLonLat(lon, lat);

        // Clear existing markers
        _locationMarkerLayer?.Features.Clear();

        // Add marker for current location
        var feature = new PointFeature(new MPoint(x, y));
        feature.Styles.Add(new SymbolStyle
        {
            Symbol = new Symbol
            {
                Type = SymbolType.Ellipse,
                Fill = new Brush(new Color(255, 0, 0, 128)), // Red with transparency
                Outline = new Pen { Color = Color.Red, Width = 2 },
                Size = 12
            }
        });
        _locationMarkerLayer?.Features.Add(feature);

        // Center map on location
        LocationMap.Map.Home = n => n.NavigateTo(new MPoint(x, y), n.Resolutions[15]); // Zoom level 15 for street level
        LocationMap.Refresh();
    }
}
