using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace FWH.Common.Chat.ViewModels;

public partial class ChatInputViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isVisible = true;

    private ChatInputModes _inputMode = ChatInputModes.Text;
    public ChatInputModes InputMode
    {
        get => _inputMode;
        set => SetProperty(ref _inputMode, value);
    }

    [ObservableProperty]
    private string? text = string.Empty;

    [ObservableProperty]
    private ChoicePayload? choices;

    [ObservableProperty]
    private ImagePayload? currentImage;

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
                        case PayloadTypes.Image:
                            var imgPayload = current.Payload as ImagePayload;
                            if (imgPayload != null && imgPayload.Image == null)
                            {
                                // Camera node - show camera capture UI
                                SetImageMode(imgPayload);
                            }
                            else
                            {
                                ClearInput();
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
        CurrentImage = null;
        InputMode = ChatInputModes.Text;
    }

    public void SetChoices(ChoicePayload choicePayload)
    {
        Choices = choicePayload;
        CurrentImage = null;
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

    public void SetImageMode(ImagePayload imagePayload)
    {
        CurrentImage = imagePayload;
        Choices = null;
        Text = string.Empty;
        InputMode = ChatInputModes.Image;
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

    [RelayCommand]
    private async Task OpenCameraAsync()
    {
        System.Diagnostics.Debug.WriteLine("ChatInputViewModel: OpenCameraAsync called - invoking CameraRequested event");
        CameraRequested?.Invoke(this, EventArgs.Empty);
        System.Diagnostics.Debug.WriteLine("ChatInputViewModel: CameraRequested event invoked");
        await Task.CompletedTask;
    }

    public void RaiseImageCaptured(byte[] imageBytes)
    {
        ImageCaptured?.Invoke(this, imageBytes);
    }

    public event EventHandler<string>? TextSubmitted;
    public event EventHandler<ChoicesItem?>? ChoiceSubmitted;
    public event EventHandler? CameraRequested;
    public event EventHandler<byte[]>? ImageCaptured;
}
