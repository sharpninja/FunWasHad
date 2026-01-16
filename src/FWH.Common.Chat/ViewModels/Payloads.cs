using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Common.Chat.ViewModels;

public enum ChatInputModes
{
    Text,
    Choice,
    Image
}

public enum PayloadTypes { Text, Image, Choice, Xaml }

public interface IPayload
{
    PayloadTypes PayloadType { get; }
}

public partial class TextPayload(string message = "Empty Message")
    : ObservableObject, IPayload
{
    public PayloadTypes PayloadType => PayloadTypes.Text;

    [ObservableProperty]
    public string text = message;
}

public partial class ImagePayload : ObservableObject, IPayload
{
    public PayloadTypes PayloadType => PayloadTypes.Image;

    [ObservableProperty]
    public byte[]? image; 

    [ObservableProperty]
    public bool showBorder = false;
}

public partial class ChoicePayload(IEnumerable<ChoicesItem> choiceItems)
    : ObservableObject, IPayload
{
    public PayloadTypes PayloadType => PayloadTypes.Choice;

    [ObservableProperty]
    public string prompt = "Select One...";

    [ObservableProperty]
    public string title = "You selected...";

    [ObservableProperty]
    public ObservableCollection<ChoicesItem> choices = new ObservableCollection<ChoicesItem>(choiceItems);

    public ChoicesItem AddChoice(string text, object? value, int? atIndex)
    {
        if (Choices.Any(c => c.DisplayOrder == atIndex))
        {
            atIndex = null;
        }
        var newItem = new ChoicesItem(atIndex ?? Choices.Count, text, value);
        Choices.Add(newItem);
        return newItem;
    }

    [ObservableProperty]
    public ChoicesItem? selectedChoice;
}
