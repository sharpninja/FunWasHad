using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace FWH.Mobile.Logging;

public sealed class AvaloniaLogStore
{
    private readonly int _maxEntries;

    public AvaloniaLogStore(int maxEntries = 1000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);

        _maxEntries = maxEntries;
    }

    public ObservableCollection<AvaloniaLogEntry> Entries { get; } = new();

    public void Add(AvaloniaLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (Dispatcher.UIThread.CheckAccess())
        {
            AddOnUiThread(entry);
            return;
        }

        Dispatcher.UIThread.Post(() => AddOnUiThread(entry));
    }

    private void AddOnUiThread(AvaloniaLogEntry entry)
    {
        Entries.Add(entry);
        while (Entries.Count > _maxEntries)
            Entries.RemoveAt(0);
    }

    public void Clear()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Entries.Clear();
            return;
        }

        Dispatcher.UIThread.Post(() => Entries.Clear());
    }
}

