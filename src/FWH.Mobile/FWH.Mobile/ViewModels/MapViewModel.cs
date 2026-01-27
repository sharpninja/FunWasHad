using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for MapView that manages map display settings including rotation.
/// </summary>
public partial class MapViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the MovementStateViewModel that provides location and movement state data.
    /// </summary>
    [ObservableProperty]
    private MovementStateViewModel? _movementStateViewModel;

    /// <summary>
    /// Gets or sets the map rotation in degrees. 0 = north up.
    /// </summary>
    [ObservableProperty]
    private double _rotation = 0;

    /// <summary>
    /// Gets or sets whether rotation gestures are locked (disabled).
    /// When true, users cannot rotate the map via gestures.
    /// </summary>
    [ObservableProperty]
    private bool _rotationLock = true;

    public MapViewModel(MovementStateViewModel movementStateViewModel)
    {
        MovementStateViewModel = movementStateViewModel ?? throw new ArgumentNullException(nameof(movementStateViewModel));

        // Initialize with north up and rotation locked
        Rotation = 0;
        RotationLock = true;
    }
}
