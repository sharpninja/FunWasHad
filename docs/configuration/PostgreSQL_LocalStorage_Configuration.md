# PostgreSQL Local Storage Configuration

**Date:** 2025-01-08
**Status:** ✅ **COMPLETE**
**Feature:** PostgreSQL container with persistent local storage

---

## Overview

Configured the PostgreSQL container in the Aspire AppHost to use local Docker volumes for data persistence. This ensures that database data survives container restarts and application redeployments.

---

## Changes Made

### File Modified: `FWH.AppHost/Program.cs`

**Before:**
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("funwashad");
```

**After:**
```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("funwashad-postgres-data")  // Use local volume for persistence
    .WithPgAdmin()
    .AddDatabase("funwashad");
```

### Key Addition

```csharp
.WithDataVolume("funwashad-postgres-data")
```

This configures Aspire to:
1. Create a Docker named volume called `funwashad-postgres-data`
2. Mount it to `/var/lib/postgresql/data` inside the container
3. Persist all PostgreSQL data to this local volume

---

## How It Works

### Docker Volume Management

When you run the Aspire application:

1. **First Run:**
   - Docker creates a named volume: `funwashad-postgres-data`
   - PostgreSQL initializes the database in this volume
   - All database files, tables, and data are stored in the volume

2. **Subsequent Runs:**
   - Docker reuses the existing `funwashad-postgres-data` volume
   - PostgreSQL finds existing data and continues using it
   - Your data persists across container restarts

3. **Container Deletion:**
   - Even if you delete the container, the volume remains
   - Next time you start, your data is still there

### Volume Location

**Windows:**
```
\\wsl$\docker-desktop-data\data\docker\volumes\funwashad-postgres-data\_data
```

**Linux/macOS:**
```
/var/lib/docker/volumes/funwashad-postgres-data/_data
```

---

## PostGIS Extension

The Marketing API uses **PostGIS** for efficient spatial queries on business locations. PostGIS is automatically enabled by the migration `002_add_postgis_spatial_index.sql` when available.

### Features:
- **Spatial GIST Index:** Efficient spatial queries using `location_geometry` column
- **Automatic Geometry Maintenance:** Database trigger keeps geometry in sync with latitude/longitude
- **Graceful Fallback:** API automatically falls back to bounding box queries if PostGIS is unavailable (e.g., test environments)

### Migration:
The PostGIS migration (`002_add_postgis_spatial_index.sql`) will:
1. Enable PostGIS extension (if available)
2. Add `location_geometry` column to `businesses` table
3. Create spatial GIST index for optimal query performance
4. Set up trigger to auto-update geometry from lat/lon

**Note:** Railway PostgreSQL includes PostGIS by default. For local development, ensure PostGIS is installed in your PostgreSQL instance.

## Benefits

### ✅ Data Persistence

- **Survives Restarts:** Database data persists across container restarts
- **Survives Redeployments:** Data survives application redeployments
- **Survives Updates:** Data remains when updating PostgreSQL version

### ✅ Development Workflow

- **No Data Loss:** Don't lose test data when restarting
- **Consistent State:** Database maintains state between debug sessions
- **Migration Safety:** Database migrations are preserved

### ✅ Performance

- **Native Performance:** Docker volumes use native filesystem performance
- **No Overhead:** No performance penalty compared to container filesystem
- **Efficient I/O:** Optimized for database workloads

---

## Volume Management

### View Existing Volumes

```bash
# List all Docker volumes
docker volume ls

# Inspect the PostgreSQL volume
docker volume inspect funwashad-postgres-data
```

**Output:**
```json
[
    {
        "CreatedAt": "2025-01-08T10:00:00Z",
        "Driver": "local",
        "Labels": {
            "com.docker.compose.project": "funwashad",
            "com.docker.compose.volume": "funwashad-postgres-data"
        },
        "Mountpoint": "/var/lib/docker/volumes/funwashad-postgres-data/_data",
        "Name": "funwashad-postgres-data",
        "Options": null,
        "Scope": "local"
    }
]
```

### Backup the Database

```bash
# Create a backup of the entire volume
docker run --rm \
  -v funwashad-postgres-data:/data \
  -v $(pwd):/backup \
  alpine \
  tar czf /backup/postgres-backup-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
```

### Restore from Backup

```bash
# Restore from a backup
docker run --rm \
  -v funwashad-postgres-data:/data \
  -v $(pwd):/backup \
  alpine \
  tar xzf /backup/postgres-backup-20250108-100000.tar.gz -C /data
```

### Reset Database (Delete Volume)

```bash
# Stop the application first
# Then remove the volume
docker volume rm funwashad-postgres-data

# Next run will create a fresh database
```

---

## Comparison: Volume vs Bind Mount

### Named Volume (What We're Using) ✅

```csharp
.WithDataVolume("funwashad-postgres-data")
```

**Advantages:**
- ✅ **Docker Managed** - Docker handles location and permissions
- ✅ **Cross-Platform** - Works same on Windows/Linux/macOS
- ✅ **Performance** - Optimized for database I/O
- ✅ **Simple** - No need to specify paths
- ✅ **Portable** - Easy to backup/restore

**Use When:**
- You want Docker to manage storage
- You don't need direct filesystem access
- You want best performance

### Bind Mount (Alternative)

```csharp
.WithDataBindMount("E:/docker-data/postgres")
```

**Advantages:**
- ✅ **Direct Access** - Can browse files from host
- ✅ **Specific Location** - Control exact storage location
- ✅ **Easy Backup** - Can use normal file backup tools

**Disadvantages:**
- ❌ **Platform Specific** - Paths differ between OS
- ❌ **Permission Issues** - May have permission problems
- ❌ **Performance** - Slightly slower on Windows/macOS

**Use When:**
- You need direct filesystem access
- You want to control the exact location
- You're using file-based backup tools

---

## Configuration Options

### Option 1: Named Volume (Current - Recommended)

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("funwashad-postgres-data")
    .WithPgAdmin()
    .AddDatabase("funwashad");
```

**Features:**
- Docker-managed storage
- Automatic cleanup when volume deleted
- Best for development and production

### Option 2: Bind Mount to Specific Path

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataBindMount("E:/docker-data/postgres")
    .WithPgAdmin()
    .AddDatabase("funwashad");
```

**Features:**
- Specific filesystem path
- Direct file access
- Good for manual backups

### Option 3: Anonymous Volume

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()  // No name - Docker generates one
    .WithPgAdmin()
    .AddDatabase("funwashad");
```

**Features:**
- Automatic name generation
- Harder to manage manually
- Use for temporary storage

### Option 4: No Volume (Not Persistent)

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("funwashad");
```

**Features:**
- Data lost on container removal
- Fresh database every time
- Use for testing only

---

## Migration Workflow

### Running Migrations

With persistent storage, migrations work naturally:

```bash
# Run the Location API
dotnet run --project FWH.AppHost

# Migrations are applied automatically on startup
# Database state is preserved for next run
```

### Migration Script Execution

```bash
# Check if migration SQL exists
cat FWH.Location.Api/Migrations/001_create_device_locations.sql

# Execute manually if needed (from within container)
docker exec -i funwashad-postgres-1 psql -U postgres -d funwashad < \
  FWH.Location.Api/Migrations/001_create_device_locations.sql
```

---

## Testing

### Verify Persistence

1. **Start the application:**
   ```bash
   dotnet run --project FWH.AppHost
   ```

2. **Insert test data:**
   ```bash
   # Use PgAdmin or direct connection
   # Insert some location records
   ```

3. **Stop the application:**
   ```
   Ctrl+C
   ```

4. **Restart the application:**
   ```bash
   dotnet run --project FWH.AppHost
   ```

5. **Verify data still exists:**
   - Data should still be there
   - No need to re-run migrations
   - Application continues from previous state

### Verify Volume Creation

```bash
# Check volume was created
docker volume ls | grep funwashad-postgres-data

# Expected output:
# local     funwashad-postgres-data
```

### Check Container Mounts

```bash
# List running containers
docker ps

# Inspect PostgreSQL container
docker inspect <postgres-container-id> | grep -A 10 "Mounts"
```

**Expected Output:**
```json
"Mounts": [
    {
        "Type": "volume",
        "Name": "funwashad-postgres-data",
        "Source": "/var/lib/docker/volumes/funwashad-postgres-data/_data",
        "Destination": "/var/lib/postgresql/data",
        "Driver": "local",
        "Mode": "z",
        "RW": true,
        "Propagation": ""
    }
]
```

---

## Troubleshooting

### Volume Not Created

**Symptom:** Volume doesn't appear in `docker volume ls`

**Solution:**
1. Ensure Aspire application is running
2. Check Docker is running
3. Verify no errors in AppHost startup

### Data Not Persisting

**Symptom:** Data disappears after restart

**Solution:**
1. Verify volume is configured: `.WithDataVolume("funwashad-postgres-data")`
2. Check volume still exists: `docker volume ls`
3. Ensure same volume name is used

### Permission Errors

**Symptom:** PostgreSQL can't write to volume

**Solution:**
```bash
# Usually Docker handles this automatically
# If issues persist, check volume permissions
docker volume inspect funwashad-postgres-data
```

### Volume Full

**Symptom:** Database operations fail with "no space left"

**Solution:**
```bash
# Check volume size
docker system df -v

# Clean up old data
docker volume prune

# Or increase Docker disk space allocation
```

---

## Production Considerations

### Backup Strategy

**Recommended Backup Approach:**

```bash
# Daily automated backup script
#!/bin/bash
BACKUP_DIR="/path/to/backups"
DATE=$(date +%Y%m%d-%H%M%S)

# Backup using pg_dump (recommended)
docker exec funwashad-postgres-1 pg_dump -U postgres funwashad > \
  $BACKUP_DIR/funwashad-$DATE.sql

# Or backup entire volume
docker run --rm \
  -v funwashad-postgres-data:/data \
  -v $BACKUP_DIR:/backup \
  alpine \
  tar czf /backup/postgres-vol-$DATE.tar.gz -C /data .

# Keep only last 7 days
find $BACKUP_DIR -name "funwashad-*.sql" -mtime +7 -delete
```

### Monitoring

**Monitor Volume Usage:**

```bash
# Check volume size
docker system df -v | grep funwashad-postgres-data

# Monitor database size
docker exec funwashad-postgres-1 psql -U postgres -d funwashad -c \
  "SELECT pg_size_pretty(pg_database_size('funwashad'));"
```

### High Availability

For production with high availability needs:

```csharp
// Consider using managed PostgreSQL service
// (Azure Database for PostgreSQL, AWS RDS, etc.)
// instead of containerized PostgreSQL

// Or configure PostgreSQL replication
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("funwashad-postgres-data")
    .WithEnvironment("POSTGRES_REPLICATION", "true")
    .WithPgAdmin();
```

---

## Alternative: Using Bind Mounts

If you prefer to use a specific directory path:

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithDataBindMount(@"E:\docker-data\funwashad\postgres")
    .WithPgAdmin()
    .AddDatabase("funwashad");
```

**Create the directory first:**

```bash
# Windows
mkdir E:\docker-data\funwashad\postgres

# Linux/macOS
mkdir -p /var/docker-data/funwashad/postgres
```

**Advantages:**
- Know exact location of data
- Easy to browse/backup with standard tools
- Can use network storage

**Disadvantages:**
- Path must exist before starting
- Permissions can be tricky
- Platform-specific paths

---

## Summary

### What Was Changed

✅ Added `.WithDataVolume("funwashad-postgres-data")` to PostgreSQL configuration
✅ PostgreSQL now uses persistent Docker volume for data storage
✅ Database data survives container restarts and redeployments
✅ Build verified successful

### Benefits

- **Data Persistence:** Database state is preserved
- **Development Friendly:** Don't lose test data
- **Production Ready:** Suitable for production deployments
- **Easy Management:** Docker handles storage location
- **Performance:** Native filesystem performance

### Volume Information

- **Name:** `funwashad-postgres-data`
- **Type:** Docker named volume
- **Mount Point:** `/var/lib/postgresql/data` (inside container)
- **Driver:** local
- **Managed By:** Docker

---

**Configuration Status:** ✅ **COMPLETE**
**Build Status:** ✅ **SUCCESSFUL**
**Testing:** ✅ **READY**
**Production Ready:** ✅ **YES**

---

*Document Version: 1.0*
*Date: 2025-01-08*
*Status: Complete*
