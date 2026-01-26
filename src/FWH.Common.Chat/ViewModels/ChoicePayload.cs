using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Common.Chat.ViewModels;

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
