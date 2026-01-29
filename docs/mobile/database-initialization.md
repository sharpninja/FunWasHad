# SQLite Database Initialization on Android

## Overview

The FWH Mobile application uses SQLite databases for persistent local storage across all platforms (Android, iOS, Desktop). This document explains how database initialization and persistence is implemented.

## Database Location by Platform

### Android
- **Path**: `/data/data/com.CompanyName.FWH.Mobile/files/fwh_mobile.db`
- **Obtained via**: `Android.App.Application.Context.GetExternalFilesDir(null)`
- **Permissions**: No special permissions required (uses app-private storage)

### iOS
- **Path**: `~/Library/fwh_mobile.db`
- **Obtained via**: Documents directory + `../Library`

### Desktop (Windows/Linux/macOS)
- **Path**: `%LocalAppData%/FWH.Mobile/fwh_mobile.db` (Windows)
- **Path**: `~/.local/share/FWH.Mobile/fwh_mobile.db` (Linux)
- **Path**: `~/Library/Application Support/FWH.Mobile/fwh_mobile.db` (macOS)

### Browser/WASM
- **Storage**: In-memory only (`:memory:`)
- **Note**: Data is not persisted between sessions

## Database Schema

The mobile database (`NotesDbContext`) contains the following tables:

1. **Notes** - User notes with title, content, and timestamps
2. **WorkflowDefinitions** - Workflow definitions parsed from PlantUML
3. **NodeEntities** - Workflow nodes
4. **TransitionEntities** - Workflow transitions
5. **StartPointEntities** - Workflow start points
6. **ConfigurationSettings** - App configuration key-value store
7. **DeviceLocationHistory** - Device GPS location history (TR-MOBILE-001: local-only, never sent to API)

## Initialization Process

### 1. Platform Detection
The `IPlatformService` interface provides platform detection and database path resolution:

```csharp
var platformService = new PlatformService();
var databasePath = platformService.GetDatabasePath("fwh_mobile.db");
var connectionString = $"DataSource={databasePath}";
```

### 2. Service Registration
Database services are registered during app startup in `App.axaml.cs`:

```csharp
services.AddDataServices(connectionString);
```

### 3. Database Initialization
The database is initialized asynchronously before the UI is displayed:

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    await EnsureDatabaseInitializedAsync();
    // ... rest of initialization
}
```

### 4. Migration Application
The `MobileDatabaseMigrationService` handles:
- Creating the database if it doesn't exist
- Applying any pending Entity Framework migrations
- Logging the process for debugging

## Key Components

### IPlatformService
- Interface: `FWH.Common.Chat.Services.IPlatformService`
- Implementation: `FWH.Common.Chat.Services.PlatformService`
- Purpose: Platform detection and database path resolution

### NotesDbContext
- Location: `FWH.Mobile.Data.Data.NotesDbContext`
- Purpose: Entity Framework DbContext for mobile app data
- Migration: Uses EF Core migrations

### MobileDatabaseMigrationService
- Location: `FWH.Mobile.Data.Services.MobileDatabaseMigrationService`
- Purpose: Ensures database is created and migrations are applied
- Methods:
  - `EnsureDatabaseAsync()` - Main initialization method
  - `GetAppliedMigrationsAsync()` - Lists applied migrations
  - `GetPendingMigrationsAsync()` - Lists pending migrations
  - `GetConnectionInfo()` - Returns connection string for diagnostics

## Troubleshooting

### Database Not Created
1. Check logs for database path and initialization errors
2. Verify the app has write permissions to the database directory
3. On Android, check that the app-private storage directory exists

### Database Location on Android
To find the database file on a connected Android device:
```bash
adb shell
cd /data/data/com.CompanyName.FWH.Mobile/files
ls -la fwh_mobile.db
```

To pull the database file for inspection:
```bash
adb pull /data/data/com.CompanyName.FWH.Mobile/files/fwh_mobile.db ./fwh_mobile.db
```

### Viewing Database Contents
Use any SQLite viewer (e.g., DB Browser for SQLite, Azure Data Studio with SQLite extension):
1. Pull the database file using adb (Android) or locate it in the file system (Desktop)
2. Open with SQLite viewer
3. Inspect tables and data

## Technical Requirements

The authoritative technical requirement for local device location tracking is documented in:

- `docs/Project/Technical-Requirements.md` → **TR-MOBILE-001: Local Device Location Tracking**

## Related Files

- `src/FWH.Common.Chat/Services/IPlatformService.cs` - Platform service interface
- `src/FWH.Common.Chat/Services/PlatformService.cs` - Platform service implementation
- `src/FWH.Mobile.Data/Data/NotesDbContext.cs` - DbContext definition
- `src/FWH.Mobile.Data/Services/MobileDatabaseMigrationService.cs` - Migration service
- `src/FWH.Mobile/FWH.Mobile/App.axaml.cs` - Application startup and initialization
- `src/FWH.Mobile.Data/Extensions/DataServiceCollectionExtensions.cs` - DI registration
