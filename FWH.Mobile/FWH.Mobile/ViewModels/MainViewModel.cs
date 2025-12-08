using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Mobile.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
}
