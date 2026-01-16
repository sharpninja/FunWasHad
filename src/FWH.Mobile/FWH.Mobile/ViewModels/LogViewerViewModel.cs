using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using FWH.Mobile.Logging;

namespace FWH.Mobile.ViewModels;

public sealed class LogViewerViewModel
{
    private readonly AvaloniaLogStore _store;

    public LogViewerViewModel(AvaloniaLogStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ClearCommand = new RelayCommand(_store.Clear);
    }

    public ObservableCollection<AvaloniaLogEntry> Entries => _store.Entries;

    public ICommand ClearCommand { get; }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute) => _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}

