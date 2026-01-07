using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FWH.Common.Chat.ViewModels;

public partial class ChatListViewModel(IServiceProvider? serviceProvider = null) : ViewModelBase
{
    private ObservableCollection<IChatEntry<IPayload>> entries = new ObservableCollection<IChatEntry<IPayload>>();

    public ObservableCollection<IChatEntry<IPayload>> Entries
    {
        get => entries;
        private set => SetProperty(ref entries, value);
    }

    public void AddEntry(IChatEntry<IPayload> entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

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
