using Microsoft.Data.Sqlite;

namespace FWH.Mobile.Data.Sqlite;

/// <summary>
/// Initializes SQLite database schema for mobile app local storage.
/// TR-MOBILE-001: Includes DeviceLocationHistory for local-only location tracking.
/// </summary>
public static class SqliteDbInitializer
{
    public static void EnsureDatabase(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        // Notes table
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Notes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);";
        cmd.ExecuteNonQuery();

        // DeviceLocationHistory table
        // TR-MOBILE-001: Device location tracked locally, never sent to API
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS DeviceLocationHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    DeviceId TEXT NOT NULL,
    Latitude REAL NOT NULL,
    Longitude REAL NOT NULL,
    AccuracyMeters REAL,
    AltitudeMeters REAL,
    SpeedMetersPerSecond REAL,
    HeadingDegrees REAL,
    MovementState TEXT NOT NULL,
    Timestamp TEXT NOT NULL,
    Address TEXT,
    CreatedAt TEXT NOT NULL
);";
        cmd.ExecuteNonQuery();

        // Create indexes for efficient querying
        cmd.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_DeviceLocationHistory_DeviceId 
    ON DeviceLocationHistory(DeviceId);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_DeviceLocationHistory_Timestamp 
    ON DeviceLocationHistory(Timestamp);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
CREATE INDEX IF NOT EXISTS IX_DeviceLocationHistory_DeviceId_Timestamp 
    ON DeviceLocationHistory(DeviceId, Timestamp);";
        cmd.ExecuteNonQuery();
    }
}

