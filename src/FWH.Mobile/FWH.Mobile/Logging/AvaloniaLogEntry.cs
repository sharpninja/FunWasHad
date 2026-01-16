using System;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Logging;

public sealed record AvaloniaLogEntry(
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
}

