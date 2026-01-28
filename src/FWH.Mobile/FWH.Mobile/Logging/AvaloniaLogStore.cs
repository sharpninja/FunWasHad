using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Avalonia.Threading;

namespace FWH.Mobile.Logging;

public sealed class AvaloniaLogStore
{
    private readonly int _maxEntries;
    private readonly Timer _purgeTimer;

    public AvaloniaLogStore(int maxEntries = 1000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);

        _maxEntries = maxEntries;

        // Periodically purge verbose log levels so we only keep
        // the most recent N entries per level (Information/Debug/Trace).
        _purgeTimer = new Timer(
            _ => PurgeSoftLevels(),
            state: null,
            dueTime: TimeSpan.FromSeconds(30),
            period: TimeSpan.FromSeconds(30));
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

        // Global safety cap for total entries.
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

    /// <summary>
    /// Purge Information, Debug, and Trace entries so that we only keep
    /// the most recent 100 of each level.
    /// Runs periodically via an internal timer.
    /// </summary>
    private void PurgeSoftLevels()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            PurgeSoftLevelsOnUiThread();
        }
        else
        {
            Dispatcher.UIThread.Post(PurgeSoftLevelsOnUiThread);
        }
    }

    private void PurgeSoftLevelsOnUiThread()
    {
        if (Entries.Count == 0)
            return;

        const int perLevelLimit = 100;
        var levelsToPurge = new[]
        {
            Microsoft.Extensions.Logging.LogLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Trace
        };

        foreach (var level in levelsToPurge)
        {
            // Capture indices for this level, in insertion order (oldest first).
            var indices = Entries
                .Select((entry, index) => (entry, index))
                .Where(x => x.entry.Level == level)
                .Select(x => x.index)
                .ToList();

            if (indices.Count <= perLevelLimit)
                continue;

            var toRemoveCount = indices.Count - perLevelLimit;

            // Remove oldest entries for this level.
            for (var i = 0; i < toRemoveCount; i++)
            {
                var index = indices[i] - i; // adjust for prior removals
                if (index >= 0 && index < Entries.Count)
                {
                    Entries.RemoveAt(index);
                }
            }
        }
    }
}

