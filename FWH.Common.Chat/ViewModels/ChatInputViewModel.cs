using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace FWH.Common.Chat.ViewModels;

public partial class ChatInputViewModel : ViewModelBase
{
    [ObservableProperty]
    public bool isVisible = true;

    private ChatInputModes _inputMode = ChatInputModes.Text;
    public ChatInputModes InputMode
    {
        get => _inputMode;
        set => SetProperty(ref _inputMode, value);
    }

    [ObservableProperty]
    public string? text = string.Empty;

    [ObservableProperty]
    public ChoicePayload? choices;

    public ChatInputViewModel(ChatListViewModel listViewModel)
    {
        listViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ChatListViewModel.Current))
            {
                var current = listViewModel.Current;
                
                if (current != null)
                {
                    switch (current.Payload.PayloadType)
                    {
                        case PayloadTypes.Choice:
                            var choicePayload = current.Payload as ChoicePayload;
                            if (choicePayload != null)
                            {
                                SetChoices(choicePayload);
                            }
                            break;
                        default:
                            ClearInput();
                            break;
                    }
                }
            }
        };
    }

    public void ClearInput()
    {
        Text = string.Empty;
        Choices = null;
        InputMode = ChatInputModes.Text;
    }

    public void SetChoices(ChoicePayload choicePayload)
    {
        Choices = choicePayload;
        InputMode = ChatInputModes.Choice;

        foreach (var choice in choicePayload.Choices)
        {
            void submitted(object? s, ChoicesItem? e)
            {
                ChoiceSubmitted?.Invoke(this, e);
                choice.ChoiceSubmitted -= submitted;
            }

            choice.ChoiceSubmitted += submitted;
        }
    }

    [RelayCommand]
    private void Send()
    {
        switch (InputMode)
        {
            case ChatInputModes.Text:
                TextSubmitted?.Invoke(this, Text ?? string.Empty);
                Text = string.Empty; // Clear after sending
                break;
        }
    }

    public event EventHandler<string>? TextSubmitted;
    public event EventHandler<ChoicesItem?>? ChoiceSubmitted;
}
