using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Common.Chat.ViewModels;

public partial class ImagePayload : ObservableObject, IPayload
{
    public PayloadTypes PayloadType => PayloadTypes.Image;

    [ObservableProperty]
    public byte[]? image;

    [ObservableProperty]
    public bool showBorder = false;
}
