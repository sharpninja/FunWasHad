using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FWH.Mobile.Logging;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.ViewModels;

public sealed class LogViewerViewModel : INotifyPropertyChanged
{
    private readonly AvaloniaLogStore _store;
    private LogLevel _selectedLogLevel = LogLevel.Trace;
    private readonly ObservableCollection<AvaloniaLogEntry> _filteredEntries = new();

    public LogViewerViewModel(AvaloniaLogStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        ClearCommand = new RelayCommand(_store.Clear);
        ScrollToEndCommand = new RelayCommand(() => ScrollToEndRequested?.Invoke());

        // Initialize filtered entries and subscribe to changes
        UpdateFilteredEntries(scrollToEnd: false);
        _store.Entries.CollectionChanged += OnEntriesCollectionChanged;
    }

    public ObservableCollection<AvaloniaLogEntry> Entries => _store.Entries;

    public ObservableCollection<AvaloniaLogEntry> FilteredEntries => _filteredEntries;

    public ICommand ClearCommand { get; }

    public ICommand ScrollToEndCommand { get; }

    public event Action? ScrollToEndRequested;

    public LogLevel SelectedLogLevel
    {
        get => _selectedLogLevel;
        set
        {
            if (_selectedLogLevel != value)
            {
                _selectedLogLevel = value;
                OnPropertyChanged();
                UpdateFilteredEntries(scrollToEnd: true);
            }
        }
    }

    public LogLevel[] LogLevels { get; } = new[]
    {
        LogLevel.Trace,
        LogLevel.Debug,
        LogLevel.Information,
        LogLevel.Warning,
        LogLevel.Error,
        LogLevel.Critical
    };

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // Only add new entries that match the filter
            var newEntries = e.NewItems.Cast<AvaloniaLogEntry>()
                .Where(entry => entry.Level >= _selectedLogLevel)
                .ToList();

            foreach (var entry in newEntries)
            {
                _filteredEntries.Add(entry);
            }

            // Scroll to end when new entries are added
            if (newEntries.Any())
            {
                ScrollToEndRequested?.Invoke();
            }
        }
        else
        {
            // Rebuild filtered list for any other collection change (Reset, Remove, Replace, Move)
            UpdateFilteredEntries(scrollToEnd: false);
        }
    }

    private void UpdateFilteredEntries(bool scrollToEnd = false)
    {
        // Clear and rebuild filtered list to maintain order and filter correctly
        _filteredEntries.Clear();

        foreach (var entry in _store.Entries.Where(entry => entry.Level >= _selectedLogLevel))
        {
            _filteredEntries.Add(entry);
        }

        // Only scroll to end if explicitly requested (e.g., when filter changes)
        if (scrollToEnd && _filteredEntries.Count > 0)
        {
            ScrollToEndRequested?.Invoke();
        }
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute) => _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
