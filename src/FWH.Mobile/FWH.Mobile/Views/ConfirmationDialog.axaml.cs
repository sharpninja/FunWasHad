using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FWH.Mobile.Views;

/// <summary>
/// Simple confirmation dialog window.
/// </summary>
public partial class ConfirmationDialog : Window
{
    private ConfirmationDialogViewModel? _viewModel;
    public bool? Result { get; private set; }

    public string Title
    {
        get => _viewModel?.Title ?? "Confirm";
        set
        {
            if (_viewModel != null)
                _viewModel.Title = value;
        }
    }

    public string Message
    {
        get => _viewModel?.Message ?? "";
        set
        {
            if (_viewModel != null)
                _viewModel.Message = value;
        }
    }

    public string ConfirmText
    {
        get => _viewModel?.ConfirmText ?? "OK";
        set
        {
            if (_viewModel != null)
                _viewModel.ConfirmText = value;
        }
    }

    public string CancelText
    {
        get => _viewModel?.CancelText ?? "Cancel";
        set
        {
            if (_viewModel != null)
                _viewModel.CancelText = value;
        }
    }

    public ConfirmationDialog()
    {
        InitializeComponent();
        _viewModel = new ConfirmationDialogViewModel(this);
        DataContext = _viewModel;
        
        // Update window title when ViewModel title changes
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConfirmationDialogViewModel.Title))
            {
                Title = _viewModel.Title;
            }
        };
        Title = _viewModel.Title;
    }

    public async Task<bool?> ShowDialogAsync(Window? owner = null)
    {
        if (owner == null)
        {
            var lifetime = App.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            owner = lifetime?.MainWindow;
        }

        if (owner != null)
        {
            Owner = owner;
        }

        await ShowDialog<bool?>(owner);
        return Result;
    }

    internal void SetResult(bool result)
    {
        Result = result;
        Close();
    }
}

public partial class ConfirmationDialogViewModel : ObservableObject
{
    private readonly ConfirmationDialog _dialog;

    [ObservableProperty]
    private string _title = "Confirm";

    [ObservableProperty]
    private string _message = "";

    [ObservableProperty]
    private string _confirmText = "OK";

    [ObservableProperty]
    private string _cancelText = "Cancel";

    public ConfirmationDialogViewModel(ConfirmationDialog dialog)
    {
        _dialog = dialog;
    }

    [RelayCommand]
    private void Confirm()
    {
        _dialog.SetResult(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        _dialog.SetResult(false);
    }
}
