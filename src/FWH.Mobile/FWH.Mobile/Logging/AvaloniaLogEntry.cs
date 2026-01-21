using System;
using Avalonia.Media;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Logging;

public sealed record AvaloniaLogEntry(
    long Id,
    DateTimeOffset TimestampUtc,
    LogLevel Level,
    string Category,
    EventId EventId,
    string Message,
    Exception? Exception)
{
    public string LevelShort => Level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => Level.ToString()
    };

    public IBrush LevelColor => Level switch
    {
        LogLevel.Trace => Brushes.Gray,
        LogLevel.Debug => Brushes.Cyan,
        LogLevel.Information => Brushes.White,
        LogLevel.Warning => Brushes.Yellow,
        LogLevel.Error => Brushes.Red,
        LogLevel.Critical => Brushes.Magenta,
        _ => Brushes.White
    };

    public IBrush LevelBackgroundColor => Level switch
    {
        LogLevel.Trace => new SolidColorBrush(Color.FromRgb(40, 40, 40)),      // Dark gray
        LogLevel.Debug => new SolidColorBrush(Color.FromRgb(0, 60, 80)),       // Dark cyan/teal
        LogLevel.Information => new SolidColorBrush(Color.FromRgb(20, 40, 60)), // Dark blue-gray
        LogLevel.Warning => new SolidColorBrush(Color.FromRgb(100, 80, 0)),    // Dark yellow/orange
        LogLevel.Error => new SolidColorBrush(Color.FromRgb(80, 0, 0)),        // Dark red
        LogLevel.Critical => new SolidColorBrush(Color.FromRgb(80, 0, 60)),    // Dark magenta/purple
        _ => new SolidColorBrush(Color.FromRgb(30, 30, 30))                    // Default dark
    };
}

