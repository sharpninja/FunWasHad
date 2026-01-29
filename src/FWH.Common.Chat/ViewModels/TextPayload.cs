using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Common.Chat.ViewModels;

public partial class TextPayload(string message = "Empty Message")
    : ObservableObject, IPayload
{
    public PayloadTypes PayloadType => PayloadTypes.Text;

    [ObservableProperty]
    public string text = message;
}
