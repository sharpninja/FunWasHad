using Microsoft.Data.Sqlite;

namespace FWH.Mobile.Data.Sqlite;

public static class SqliteDbInitializer
{
    public static void EnsureDatabase(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Notes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);";
        cmd.ExecuteNonQuery();
    }
}
