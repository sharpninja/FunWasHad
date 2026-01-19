using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace FWH.Common.Chat.ViewModels;

public partial class ChoicesItem(int order, string text, object? value)
    : ObservableObject
{
    [ObservableProperty]
    private int displayOrder = order;
    [ObservableProperty]
    private string choiceText = text;
    [ObservableProperty]
    private bool isSelected = false;
    [ObservableProperty]
    private object? choiceValue = value;

    public event EventHandler<ChoicesItem?>? ChoiceSubmitted;


    [RelayCommand(AllowConcurrentExecutions =false)]
    private Task SelectChoice(ChoicesItem? choice)
    {
        Console.WriteLine($"[ChoicesItem] SelectChoice invoked Order={DisplayOrder} Text={ChoiceText} Value={ChoiceValue}");
        ChoiceSubmitted?.Invoke(this, choice);
        return Task.CompletedTask;
    }
}
