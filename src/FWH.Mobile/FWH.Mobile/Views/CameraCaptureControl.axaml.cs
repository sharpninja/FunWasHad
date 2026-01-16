using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FWH.Mobile.Views;

public partial class CameraCaptureControl : UserControl
{
    public CameraCaptureControl()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetService(typeof(ViewModels.CameraCaptureViewModel));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
