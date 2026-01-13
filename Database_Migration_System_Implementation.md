# Database Migration System Implementation

**Date:** 2025-01-08  
**Status:** ✅ **COMPLETE**  
**Feature:** Automatic database migrations on application startup

---

## Overview

Implemented an automatic database migration system for the Location API that applies SQL migration scripts on application startup. The system ensures the database is initialized, tracks applied migrations, and executes pending migrations in order.

---

## Components Created

### 1. DatabaseMigrationService ✅

**File:** `FWH.Location.Api/Data/DatabaseMigrationService.cs`

**Purpose:** Manages database migrations and ensures database initialization

**Features:**
- ✅ **Database Creation** - Creates database if it doesn't exist
- ✅ **Migration Tracking** - Tracks applied migrations in `__migrations` table
- ✅ **Sequential Execution** - Applies migrations in alphabetical order
- ✅ **Transactional** - Each migration runs in a transaction (rollback on failure)
- ✅ **Idempotent** - Safe to run multiple times (skips already applied migrations)
- ✅ **Comprehensive Logging** - Detailed logging at each step
- ✅ **Error Handling** - Graceful error handling with rollback

**Key Methods:**

```csharp
public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    // Main method that orchestrates the migration process

private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    // Creates the database if it doesn't exist

private async Task EnsureMigrationsTableExistsAsync(CancellationToken cancellationToken)
    // Creates the __migrations tracking table

private async Task<HashSet<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
    // Gets list of already applied migrations

private List<MigrationScript> GetMigrationScripts()
    // Loads migration scripts from Migrations folder

private async Task ApplyMigrationAsync(MigrationScript migration, CancellationToken cancellationToken)
    // Applies a single migration with transaction support
```

---

### 2. Automatic Migration on Startup ✅

**File:** `FWH.Location.Api/Program.cs`

**Changes:**
```csharp
var app = builder.Build();

// Apply database migrations on startup
await ApplyDatabaseMigrationsAsync(app);

// ... rest of application setup
```

**Migration Helper Method:**
```csharp
static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Get connection string
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("funwashad");
        
        // Create and run migration service
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseMigrationService>>();
        var migrationService = new DatabaseMigrationService(connectionString, migrationLogger);
        
        await migrationService.ApplyMigrationsAsync();
        
        logger.LogInformation("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }
}
```

---

### 3. Migration Files Configuration ✅

**File:** `FWH.Location.Api/FWH.Location.Api.csproj`

**Added:**
```xml
<!-- Copy migration scripts to output directory -->
<ItemGroup>
  <Content Include="Migrations\*.sql">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

**Purpose:** Ensures migration SQL files are copied to the output directory during build

---

## Migration Tracking

### Migrations Table Schema

The system creates a `__migrations` table to track applied migrations:

```sql
CREATE TABLE IF NOT EXISTS __migrations (
    id SERIAL PRIMARY KEY,
    migration_name VARCHAR(255) NOT NULL UNIQUE,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
)
```

**Columns:**
- `id` - Auto-incrementing primary key
- `migration_name` - Name of the migration file (e.g., `001_create_device_locations.sql`)
- `applied_at` - Timestamp when the migration was applied (UTC)

---

## How It Works

### Startup Flow

```
1. Application Starts
   ↓
2. EnsureDatabaseExistsAsync()
   - Connects to 'postgres' database
   - Checks if target database exists
   - Creates database if needed
   ↓
3. EnsureMigrationsTableExistsAsync()
   - Creates __migrations table if not exists
   ↓
4. GetAppliedMigrationsAsync()
   - Queries __migrations table
   - Returns set of already applied migrations
   ↓
5. GetMigrationScripts()
   - Scans Migrations folder
   - Loads all .sql files
   - Orders them alphabetically
   ↓
6. For each pending migration:
   - Begin Transaction
   - Execute migration SQL
   - Record in __migrations table
   - Commit Transaction
   ↓
7. Application Continues Startup
```

### Migration Execution

**Transaction Flow:**
```csharp
BEGIN TRANSACTION
    ↓
Execute Migration SQL
    ↓
INSERT INTO __migrations (migration_name) VALUES (...)
    ↓
COMMIT TRANSACTION
```

**On Error:**
```csharp
BEGIN TRANSACTION
    ↓
Execute Migration SQL
    ↓
❌ Error Occurs
    ↓
ROLLBACK TRANSACTION
    ↓
Log Error & Throw Exception
    ↓
Application Startup Fails
```

---

## Existing Migrations

### 001_create_device_locations.sql ✅

**File:** `FWH.Location.Api/Migrations/001_create_device_locations.sql`

**Creates:**
- `device_locations` table
- Indexes for efficient querying
- Check constraints for data validation
- Comments for documentation

**Table Structure:**
```sql
CREATE TABLE IF NOT EXISTS device_locations (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(100) NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    accuracy_meters DOUBLE PRECISION,
    timestamp TIMESTAMPTZ NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    
    CONSTRAINT device_locations_latitude_check CHECK (latitude >= -90 AND latitude <= 90),
    CONSTRAINT device_locations_longitude_check CHECK (longitude >= -180 AND longitude <= 180),
    CONSTRAINT device_locations_accuracy_check CHECK (accuracy_meters IS NULL OR accuracy_meters >= 0)
);
```

**Indexes Created:**
- `idx_device_locations_device_id` - Query by device
- `idx_device_locations_timestamp` - Query by time
- `idx_device_locations_recorded_at` - Query by record time
- `idx_device_locations_device_timestamp` - Composite index for device + time queries

---

## Adding New Migrations

### Migration Naming Convention

**Format:** `NNN_description.sql`

**Examples:**
- `001_create_device_locations.sql`
- `002_add_movement_state_column.sql`
- `003_create_activity_tracking_table.sql`

**Rules:**
1. ✅ **Sequential Numbers** - Use 3-digit prefix (001, 002, 003, etc.)
2. ✅ **Descriptive Name** - Clear description of what the migration does
3. ✅ **Snake Case** - Use underscores between words
4. ✅ **SQL Extension** - Must end with `.sql`

### Creating a New Migration

**Step 1:** Create SQL file in `Migrations` folder

```sql
-- Migration: Add movement state tracking
-- Date: 2025-01-08
-- Description: Adds movement_state column to device_locations

ALTER TABLE device_locations
ADD COLUMN IF NOT EXISTS movement_state VARCHAR(20);

-- Add index for movement state queries
CREATE INDEX IF NOT EXISTS idx_device_locations_movement_state
ON device_locations(movement_state);

-- Add comment
COMMENT ON COLUMN device_locations.movement_state IS 'Current movement state: Walking, Riding, Stationary';
```

**Step 2:** Save as `002_add_movement_state.sql` in `Migrations` folder

**Step 3:** Build and run application

```powershell
# Build copies migration to output
dotnet build FWH.Location.Api

# Run application (migrations apply automatically)
dotnet run --project FWH.AppHost
```

**Step 4:** Verify in logs

```
[INFO] Starting database migration process
[INFO] Found 1 previously applied migrations
[INFO] Found 2 migration script(s)
[INFO] Applying 1 pending migration(s)
[INFO] Applying migration: 002_add_movement_state.sql
[INFO] Migration '002_add_movement_state.sql' applied successfully
[INFO] Database migration process completed successfully
```

---

## Best Practices

### ✅ DO

1. **Use Idempotent Scripts**
   ```sql
   CREATE TABLE IF NOT EXISTS my_table ...
   CREATE INDEX IF NOT EXISTS my_index ...
   ALTER TABLE ... ADD COLUMN IF NOT EXISTS ...
   ```

2. **Include Comments**
   ```sql
   -- Migration: Brief description
   -- Date: YYYY-MM-DD
   -- Description: Detailed explanation
   ```

3. **Add Database Comments**
   ```sql
   COMMENT ON TABLE my_table IS 'Description';
   COMMENT ON COLUMN my_table.my_column IS 'Description';
   ```

4. **Create Indexes**
   ```sql
   CREATE INDEX IF NOT EXISTS idx_table_column ON table(column);
   ```

5. **Add Constraints**
   ```sql
   CONSTRAINT table_column_check CHECK (column >= 0)
   ```

### ❌ DON'T

1. **Don't Drop Tables**
   ```sql
   -- ❌ Bad: Destructive
   DROP TABLE IF EXISTS my_table;
   
   -- ✅ Good: Use ALTER or conditional CREATE
   CREATE TABLE IF NOT EXISTS my_table ...
   ```

2. **Don't Modify Existing Migrations**
   ```
   ❌ Don't edit 001_create_device_locations.sql after it's applied
   ✅ Create 002_modify_device_locations.sql instead
   ```

3. **Don't Skip Numbers**
   ```
   ❌ 001, 003, 004 (missing 002)
   ✅ 001, 002, 003, 004
   ```

4. **Don't Use Special Characters**
   ```
   ❌ 001-create table!.sql
   ✅ 001_create_table.sql
   ```

---

## Testing

### Verify Migration System

**Test 1: Fresh Database**
```powershell
# Remove existing volume
docker volume rm funwashad-postgres-data

# Run application
dotnet run --project FWH.AppHost

# Check logs - should see:
# [INFO] Database 'funwashad' does not exist. Creating it...
# [INFO] Database 'funwashad' created successfully
# [INFO] Applying 1 pending migration(s)
# [INFO] Migration '001_create_device_locations.sql' applied successfully
```

**Test 2: Existing Database**
```powershell
# Run application again
dotnet run --project FWH.AppHost

# Check logs - should see:
# [INFO] Database 'funwashad' already exists
# [INFO] Found 1 previously applied migrations
# [INFO] No pending migrations to apply
```

**Test 3: New Migration**
```powershell
# Add new migration file
# Run application
dotnet run --project FWH.AppHost

# Check logs - should see:
# [INFO] Found 1 previously applied migrations
# [INFO] Found 2 migration script(s)
# [INFO] Applying 1 pending migration(s)
# [INFO] Migration '002_new_migration.sql' applied successfully
```

### Verify Database

**Connect to PostgreSQL:**
```bash
# Using docker exec
docker exec -it funwashad-postgres-1 psql -U postgres -d funwashad

# Or using PgAdmin
# URL: http://localhost:5050
```

**Check Tables:**
```sql
-- List all tables
\dt

-- Should show:
-- public | __migrations       | table
-- public | device_locations   | table

-- View migrations table
SELECT * FROM __migrations ORDER BY id;

-- Should show:
-- id | migration_name                      | applied_at
-- 1  | 001_create_device_locations.sql     | 2025-01-08 10:00:00+00
```

**Check Table Structure:**
```sql
-- Describe device_locations table
\d device_locations

-- Check indexes
\di device_locations*

-- Check constraints
SELECT conname, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = 'device_locations'::regclass;
```

---

## Logging Output

### Successful Migration

```
[INFO] Checking for database migrations...
[INFO] Starting database migration process
[INFO] Database 'funwashad' already exists
[DEBUG] Migrations tracking table verified
[INFO] Found 1 previously applied migrations
[INFO] Found 2 migration script(s)
[INFO] Applying 1 pending migration(s)
[INFO] Applying migration: 002_add_movement_state.sql
[INFO] Migration '002_add_movement_state.sql' applied successfully
[INFO] Database migration process completed successfully
[INFO] Database migrations completed successfully
```

### No Pending Migrations

```
[INFO] Checking for database migrations...
[INFO] Starting database migration process
[INFO] Database 'funwashad' already exists
[DEBUG] Migrations tracking table verified
[INFO] Found 2 previously applied migrations
[INFO] Found 2 migration script(s)
[INFO] No pending migrations to apply
[INFO] Database migration process completed successfully
[INFO] Database migrations completed successfully
```

### Migration Error

```
[INFO] Checking for database migrations...
[INFO] Starting database migration process
[INFO] Database 'funwashad' already exists
[DEBUG] Migrations tracking table verified
[INFO] Found 1 previously applied migrations
[INFO] Found 2 migration script(s)
[INFO] Applying 1 pending migration(s)
[INFO] Applying migration: 002_bad_migration.sql
[ERROR] Error applying migration '002_bad_migration.sql'
PostgresException: 42P01: relation "nonexistent_table" does not exist
[ERROR] Failed to apply database migrations
[CRITICAL] Application startup aborted
```

---

## Rollback Strategy

### Automatic Rollback

Each migration runs in a transaction:
```csharp
BEGIN TRANSACTION
    Execute Migration
    Record Migration
COMMIT TRANSACTION
```

**On Error:**
- Transaction is automatically rolled back
- Database remains in previous state
- Application startup fails
- No partial migrations applied

### Manual Rollback

If you need to undo a migration:

**Option 1: Create Reverse Migration**
```sql
-- 003_revert_movement_state.sql
ALTER TABLE device_locations DROP COLUMN IF EXISTS movement_state;
DROP INDEX IF EXISTS idx_device_locations_movement_state;
```

**Option 2: Remove from Tracking (Not Recommended)**
```sql
-- Connect to database
DELETE FROM __migrations WHERE migration_name = '002_add_movement_state.sql';
-- Then manually revert changes
ALTER TABLE device_locations DROP COLUMN movement_state;
```

**Option 3: Reset Database**
```powershell
# Remove volume and start fresh
.\scripts\Clean-DockerResources.ps1 -Volumes -Force
dotnet run --project FWH.AppHost
```

---

## Production Considerations

### Backup Before Migration

```powershell
# Always backup before deploying new migrations
.\scripts\Backup-Database.ps1

# Deploy new version
# If problems occur, restore
.\scripts\Restore-Database.ps1 -BackupFile ".\backups\postgres-backup-YYYYMMDD-HHMMSS.tar.gz"
```

### Migration Timing

**Good Times:**
- During maintenance windows
- Low traffic periods
- After backup

**Bad Times:**
- Peak usage hours
- Without backups
- During critical operations

### Large Migrations

For migrations that modify large tables:

```sql
-- Add column with default (fast)
ALTER TABLE device_locations 
ADD COLUMN IF NOT EXISTS new_column VARCHAR(50) DEFAULT 'default_value';

-- Create index concurrently (doesn't lock table)
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_new_column 
ON device_locations(new_column);

-- Update in batches (if needed)
UPDATE device_locations 
SET new_column = 'calculated_value' 
WHERE id BETWEEN 1 AND 1000;
-- Repeat for other ranges
```

### Monitoring

Monitor migration duration:
```
[INFO] Applying migration: 002_add_movement_state.sql
[INFO] Migration '002_add_movement_state.sql' applied successfully (125ms)
```

If migrations take > 30 seconds:
- Consider splitting into smaller migrations
- Use background jobs for data updates
- Add indexes separately

---

## Troubleshooting

### Migration Not Applied

**Symptom:** Migration file exists but isn't applied

**Check:**
1. File is in `Migrations` folder
2. File has `.sql` extension
3. File is copied to output directory (`bin/Debug/net9.0/Migrations/`)
4. Check application logs for errors

### Connection String Not Found

**Error:** `Database connection string 'funwashad' not found`

**Solution:**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "funwashad": "Host=localhost;Database=funwashad;Username=postgres;Password=..."
  }
}
```

Or check Aspire configuration in AppHost.

### Permission Denied

**Error:** `permission denied to create database`

**Solution:**
- Ensure PostgreSQL user has CREATEDB permission
- Or create database manually first

### Migration Already Applied

**Symptom:** Migration applied twice

**Check:**
```sql
SELECT * FROM __migrations ORDER BY id;
```

**Fix:**
```sql
-- If duplicate, remove one
DELETE FROM __migrations WHERE id = <duplicate_id>;
```

---

## Summary

### What Was Implemented

✅ **DatabaseMigrationService** - Full migration management system  
✅ **Automatic Startup Migrations** - Runs on app start  
✅ **Migration Tracking** - `__migrations` table  
✅ **Transaction Safety** - Rollback on error  
✅ **Comprehensive Logging** - Detailed progress information  
✅ **File Configuration** - Migrations copied to output  
✅ **Database Creation** - Creates DB if not exists  

### Benefits

- ✅ **Zero Manual Setup** - Database initializes automatically
- ✅ **Version Control** - Migrations tracked in `__migrations` table
- ✅ **Safe Deployment** - Transactional with rollback
- ✅ **Easy Testing** - Reset and reapply anytime
- ✅ **Production Ready** - Error handling and logging

### Next Steps

1. Add more migrations as needed
2. Monitor migration performance
3. Regular database backups
4. Document schema changes

---

**Implementation Status:** ✅ **COMPLETE**  
**Build Status:** ✅ **SUCCESSFUL**  
**Testing:** ✅ **READY**  
**Production Ready:** ✅ **YES**

---

*Document Version: 1.0*  
*Date: 2025-01-08*  
*Status: Complete*
