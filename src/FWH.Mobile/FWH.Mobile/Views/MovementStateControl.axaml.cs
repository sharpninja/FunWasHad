using Avalonia.Controls;
using FWH.Mobile.ViewModels;

namespace FWH.Mobile.Views;

/// <summary>
/// UserControl that displays real-time movement state information.
/// Shows current movement state, speed, location, and tracking status.
/// </summary>
public partial class MovementStateControl : UserControl
{
    public MovementStateControl()
    {
        InitializeComponent();
    }
}
