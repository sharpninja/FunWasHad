using Avalonia.Controls;
using FWH.Mobile.ViewModels;
using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;

namespace FWH.Mobile.Views;

/// <summary>
/// UserControl that displays real-time movement state information.
/// Shows current movement state, speed, location, and tracking status.
/// Includes a native map control showing the device location.
/// </summary>
public partial class MovementStateControl : UserControl
{
    private MovementStateViewModel? _viewModel;

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

        // Set initial view (Mapsui 5: use Navigator.CenterOnAndZoomTo)
        var resolutions = LocationMap.Map.Navigator.Resolutions;
        if (resolutions.Count > 9)
            LocationMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(0, 0), resolutions[9]);
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

        // Center map on location (Mapsui 5: use Navigator.CenterOnAndZoomTo)
        // TODO: Restore device marker when Mapsui 5 MemoryLayer API is confirmed
        var resolutions = LocationMap.Map.Navigator.Resolutions;
        if (resolutions.Count > 15)
            LocationMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), resolutions[15]);
        LocationMap.Refresh();
    }
}
