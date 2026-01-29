using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace FWH.Common.Chat.ViewModels;

#pragma warning disable CS9113 // Parameter is unread
public partial class ChatListViewModel(IServiceProvider? _ = null) : ViewModelBase
#pragma warning restore CS9113
{
    private ObservableCollection<IChatEntry<IPayload>> entries = new ObservableCollection<IChatEntry<IPayload>>();

    public ObservableCollection<IChatEntry<IPayload>> Entries
    {
        get => entries;
        private set => SetProperty(ref entries, value);
    }

    public void AddEntry(IChatEntry<IPayload> entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Central duplicate prevention: skip adding a choice entry if the last entry is an identical choice
        if (Entries.Count > 0 && entry.Payload.PayloadType == PayloadTypes.Choice && Entries[^1].Payload.PayloadType == PayloadTypes.Choice)
        {
            var lastChoice = Entries[^1].Payload as ChoicePayload;
            var newChoice = entry.Payload as ChoicePayload;
            if (lastChoice != null && newChoice != null)
            {
                if (lastChoice.Choices.Count == newChoice.Choices.Count)
                {
                    bool same = true;
                    for (int i = 0; i < lastChoice.Choices.Count; i++)
                    {
                        if (lastChoice.Choices[i].ChoiceText != newChoice.Choices[i].ChoiceText)
                        {
                            same = false;
                            break;
                        }
                    }

                    if (same)
                        return; // skip duplicate
                }
            }
        }

        Entries.Add(entry);

        switch (entry.Payload.PayloadType)
        {
            case PayloadTypes.Choice:
                var choicePayload = entry.Payload as ChoicePayload;
                if (choicePayload != null)
                {
                    choicePayload.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ChoicePayload.SelectedChoice))
                        {
                            SelectedChoice(choicePayload.SelectedChoice);
                        }
                    };

                    OnPropertyChanged(nameof(Current));
                }
                break;

            case PayloadTypes.Image:
                // Notify that Current has changed so ChatInputViewModel can detect image mode
                OnPropertyChanged(nameof(Current));
                break;

            case PayloadTypes.Text:
                // Notify that Current has changed for text entries too
                OnPropertyChanged(nameof(Current));
                break;
        }
    }

    [RelayCommand]
    private void SelectedChoice(ChoicesItem? selectedChoice)
    {
        if (selectedChoice == null) return;

        ChoiceSelected?.Invoke(this, selectedChoice);
    }

    internal void Reset()
    {
        Entries.Clear();
    }

    public event EventHandler<ChoicesItem?>? ChoiceSelected;

    public IChatEntry<IPayload>? Current
    {
        get
        {
            if (Entries.Count == 0)
                return null;
            return Entries[^1];
        }
    }
}
