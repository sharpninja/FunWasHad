# Mobile Database Migrations - Setup and Usage

## Overview

The FWH Mobile app uses Entity Framework Core migrations to manage the SQLite database schema. This ensures consistent database structure across devices and allows for seamless schema evolution.

## Database Schema

### Tables

#### 1. Notes
- Purpose: User notes storage
- Schema: Id, Title, Content, CreatedAt

#### 2. WorkflowDefinitions
- Purpose: Workflow definitions and state
- Schema: Id, Name, CreatedAt, RowVersion (optimistic concurrency)

#### 3. NodeEntities
- Purpose: Workflow nodes
- Foreign Key: WorkflowDefinitionEntityId

#### 4. TransitionEntities
- Purpose: Workflow transitions
- Foreign Key: WorkflowDefinitionEntityId

#### 5. StartPointEntities
- Purpose: Workflow start points
- Foreign Key: WorkflowDefinitionEntityId

#### 6. ConfigurationSettings
- Purpose: App configuration key-value store
- Schema: Key (PK), Value, ValueType, Category, Description, UpdatedAt
- Indexes: Category

#### 7. **DeviceLocationHistory** (NEW - TR-MOBILE-001)
- Purpose: Local device location tracking (NEVER sent to API)
- Schema: Id, DeviceId, Latitude, Longitude, AccuracyMeters, AltitudeMeters, SpeedMetersPerSecond, HeadingDegrees, MovementState, Timestamp, Address, CreatedAt
- Indexes: DeviceId, Timestamp, (DeviceId, Timestamp) composite

## Migration Files

### Current Migrations

1. **AddWorkflowRowVersion.cs**
   - Adds RowVersion column for optimistic concurrency
   - Table: WorkflowDefinitions

2. **AddDeviceLocationHistory.cs** (NEW)
   - Creates DeviceLocationHistory table
   - Adds indexes for efficient querying
   - Purpose: TR-MOBILE-001 local-only location tracking

## Migration Service

### MobileDatabaseMigrationService

**Location:** `src/FWH.Mobile.Data/Services/MobileDatabaseMigrationService.cs`

**Responsibilities:**
- Check database existence
- Apply pending migrations automatically
- Provide migration status information
- Handle errors gracefully

**Methods:**
- `EnsureDatabaseAsync()` - Main method, ensures DB exists and is up-to-date
- `GetAppliedMigrationsAsync()` - Returns list of applied migrations
- `GetPendingMigrationsAsync()` - Returns list of pending migrations
- `GetConnectionInfo()` - Returns connection string for diagnostics

### Usage in App

The migration service is called during app startup:

```csharp
// In App.axaml.cs OnFrameworkInitializationCompleted
await EnsureDatabaseInitializedAsync();

private static async Task EnsureDatabaseInitializedAsync()
{
    using var scope = ServiceProvider.CreateScope();
    var migrationService = scope.ServiceProvider
        .GetRequiredService<MobileDatabaseMigrationService>();
    
    await migrationService.EnsureDatabaseAsync();
}
```

## Creating New Migrations

### Using EF Core Tools

1. **Install tools** (one-time):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Add migration** (from solution root):
   ```bash
   dotnet ef migrations add MigrationName --project src/FWH.Mobile.Data/FWH.Mobile.Data.csproj --startup-project src/FWH.Mobile/FWH.Mobile/FWH.Mobile.csproj
   ```

3. **Review generated migration** in `src/FWH.Mobile.Data/Migrations/`

4. **Test migration**:
   ```bash
   dotnet ef database update --project src/FWH.Mobile.Data/FWH.Mobile.Data.csproj --startup-project src/FWH.Mobile/FWH.Mobile/FWH.Mobile.csproj
   ```

### Manual Migration Creation

For complex scenarios, you can create migrations manually like `AddDeviceLocationHistory.cs`:

```csharp
public partial class YourMigrationName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add your schema changes here
        migrationBuilder.CreateTable(...);
        migrationBuilder.CreateIndex(...);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse the changes
        migrationBuilder.DropTable(...);
    }
}
```

## Database Initialization Flow

```
App Startup
    ↓
OnFrameworkInitializationCompleted()
    ↓
EnsureDatabaseInitializedAsync()
    ↓
MobileDatabaseMigrationService.EnsureDatabaseAsync()
    ↓
┌─────────────────────┐
│ Database Exists?    │
└───────┬─────────────┘
        │
    No  │  Yes
        │
┌───────▼─────────┐   ┌──────────────────────────┐
│ Create Database │   │ Check Pending Migrations │
│ (All tables)    │   └────────┬─────────────────┘
└───────┬─────────┘            │
        │                      │
        │                  ┌───▼────┐
        │                  │ Any?   │
        │                  └───┬────┘
        │                      │
        │                  No  │  Yes
        │                      │
        │              ┌───────▼──────────┐
        │              │ Apply Migrations │
        │              └───────┬──────────┘
        │                      │
        └──────────┬───────────┘
                   │
            ┌──────▼──────┐
            │ DB Ready    │
            │ App Starts  │
            └─────────────┘
```

## Database Location

### Per Platform

| Platform | Location | Example |
|----------|----------|---------|
| **Android** | Internal storage | `/data/data/com.CompanyName.FWH.Mobile/files/funwashad.db` |
| **iOS** | Documents directory | `/var/mobile/Containers/Data/Application/.../Documents/funwashad.db` |
| **Desktop (Windows)** | User AppData | `%APPDATA%/FunWasHad/funwashad.db` |
| **Desktop (Linux)** | User home | `~/.local/share/FunWasHad/funwashad.db` |
| **Desktop (macOS)** | User Library | `~/Library/Application Support/FunWasHad/funwashad.db` |

### Configuration

Database path is configured in `App.axaml.cs`:

```csharp
string dbPath;
if (OperatingSystem.IsAndroid())
{
    var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
    dbPath = Path.Combine(documentsPath, "funwashad.db");
}
else
{
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var appFolder = Path.Combine(appDataPath, "FunWasHad");
    Directory.CreateDirectory(appFolder);
    dbPath = Path.Combine(appFolder, "funwashad.db");
}

var connectionString = $"Data Source={dbPath}";
services.AddDataServices(connectionString);
```

## Fallback: SqliteDbInitializer

For scenarios where migrations can't be applied (e.g., no EF Core tools), the `SqliteDbInitializer` provides direct SQL table creation:

**Location:** `src/FWH.Mobile.Data/Sqlite/SqliteDbInitializer.cs`

**Usage:**
```csharp
SqliteDbInitializer.EnsureDatabase(connectionString);
```

This creates all tables with raw SQL `CREATE TABLE IF NOT EXISTS` statements.

## Migration Best Practices

### 1. Always Test Migrations
- Test on clean database (new install)
- Test on existing database (upgrade)
- Test rollback (Down migration)

### 2. Backward Compatibility
- Don't drop columns in use
- Add columns as nullable or with defaults
- Use multiple migrations for breaking changes

### 3. Data Migrations
- Separate schema changes from data changes
- Use SQL scripts for data transformations
- Back up data before destructive operations

### 4. Index Strategy
- Index foreign keys
- Index frequently queried columns
- Use composite indexes for multi-column queries
- DeviceLocationHistory: Indexed by DeviceId, Timestamp

### 5. Mobile Considerations
- Keep migrations small and fast
- Test on low-end devices
- Consider offline scenarios
- Minimize database size

## Troubleshooting

### Migration Not Applied

**Symptom:** New table doesn't exist after app update

**Diagnostic:**
```csharp
var migrationService = serviceProvider.GetRequiredService<MobileDatabaseMigrationService>();
var applied = await migrationService.GetAppliedMigrationsAsync();
var pending = await migrationService.GetPendingMigrationsAsync();
```

**Solutions:**
1. Check migration file is included in build
2. Verify migration class is public and partial
3. Clear app data and reinstall
4. Check logs for migration errors

### Database Locked

**Symptom:** `SQLite Error 5: 'database is locked'`

**Causes:**
- Multiple DbContext instances
- Long-running transactions
- Background sync processes

**Solutions:**
1. Use scoped DbContext
2. Keep transactions short
3. Add retry logic with delays
4. Check for leaked connections

### Schema Mismatch

**Symptom:** Column doesn't exist / wrong type

**Causes:**
- Migration not applied
- Manual database modification
- Version mismatch

**Solutions:**
1. Delete database and recreate
2. Apply missing migrations
3. Use EF Core model validation

### Performance Issues

**Symptom:** Slow queries, high battery usage

**Solutions:**
1. Add indexes for query columns
2. Use compiled queries
3. Implement pagination
4. Archive old data (especially DeviceLocationHistory)

## DeviceLocationHistory Specific

### Auto-Cleanup Recommendation

Implement periodic cleanup to prevent database bloat:

```csharp
// Delete location data older than 30 days
var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
var oldLocations = dbContext.DeviceLocationHistory
    .Where(l => l.Timestamp < cutoffDate);
dbContext.DeviceLocationHistory.RemoveRange(oldLocations);
await dbContext.SaveChangesAsync();
```

### Query Examples

```csharp
// Recent locations for device
var recent = await dbContext.DeviceLocationHistory
    .Where(l => l.DeviceId == deviceId)
    .OrderByDescending(l => l.Timestamp)
    .Take(100)
    .ToListAsync();

// Locations by movement state
var stationary = await dbContext.DeviceLocationHistory
    .Where(l => l.DeviceId == deviceId)
    .Where(l => l.MovementState == "Stationary")
    .ToListAsync();

// Date range query (uses Timestamp index)
var dayLocations = await dbContext.DeviceLocationHistory
    .Where(l => l.DeviceId == deviceId)
    .Where(l => l.Timestamp >= startOfDay && l.Timestamp < endOfDay)
    .OrderBy(l => l.Timestamp)
    .ToListAsync();
```

## Related Documentation

- [Device Location Tracking Architecture](./device-location-tracking.md)
- [Device Location Tracking Test Results](./device-location-tracking-test-results.md)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)

## Version History

- **v1.0**: Initial Notes, Workflow tables
- **v1.1**: Added ConfigurationSettings table
- **v1.2**: Added RowVersion for optimistic concurrency
- **v2.0**: Added DeviceLocationHistory (TR-MOBILE-001) ✅

---

**Status:** ✅ Complete  
**Last Updated:** January 16, 2025  
**Maintainer:** Development Team
