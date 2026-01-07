using Avalonia.Controls;
using FWH.Mobile.ViewModels;
using FWH.Common.Chat.ViewModels;

namespace FWH.Mobile.Views;
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public FWH.Common.Chat.ViewModels.ChatViewModel? ViewModel
    {
        get => DataContext as FWH.Common.Chat.ViewModels.ChatViewModel;
        set => DataContext = value;
    }
}
