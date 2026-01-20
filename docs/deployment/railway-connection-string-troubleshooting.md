# Railway Connection String Troubleshooting

## Error: "Format of the initialization string does not conform to specification"

This error occurs when the database connection string is empty, null, or malformed when the application starts.

### Root Cause

The connection string environment variable in Railway is not being resolved correctly. Railway uses template variables like `${{Postgres.DATABASE_URL}}` that must match the exact service name.

### Symptoms

- Application crashes on startup
- Error: `System.ArgumentException: Format of the initialization string does not conform to specification starting at index 0`
- Error occurs in `DatabaseMigrationService.EnsureDatabaseExistsAsync`

### Solution

#### Step 1: Verify PostgreSQL Service Name

1. Go to your Railway project: https://railway.com/project/{PROJECT_ID}
2. Find your PostgreSQL service
3. **Note the exact service name** (it might be `Postgres`, `postgres`, `PostgreSQL`, or a custom name)

#### Step 2: Check Environment Variables

1. Go to your Location API service in Railway
2. Click **"Variables"** tab
3. Look for `ConnectionStrings__funwashad`
4. Verify it matches your PostgreSQL service name:

```bash
# If your PostgreSQL service is named "Postgres":
ConnectionStrings__funwashad=${{Postgres.DATABASE_URL}}

# If your PostgreSQL service is named "postgres":
ConnectionStrings__funwashad=${{postgres.DATABASE_URL}}

# If your PostgreSQL service has a custom name, use that exact name:
ConnectionStrings__funwashad=${{YourServiceName.DATABASE_URL}}
```

#### Step 3: Verify Service Reference

The service name in `${{ServiceName.DATABASE_URL}}` **must exactly match** the PostgreSQL service name in Railway (case-sensitive).

**Common Issues:**
- Service name is `Postgres` but variable uses `postgres` (case mismatch)
- Service name is `PostgreSQL` but variable uses `Postgres`
- Service was renamed but environment variable wasn't updated

#### Step 4: Test Connection String Resolution

After updating the environment variable:

1. **Redeploy the service** in Railway
2. Check the deployment logs
3. Look for the log message: `Connection string found (length: X characters)`
4. If you see an error about unresolved template, the service name still doesn't match

### Verification

After fixing the connection string, you should see in the logs:

```
info: Program[0]
      Checking for database migrations...
info: Program[0]
      Connection string found (length: XXX characters)
info: FWH.Location.Api.Data.DatabaseMigrationService[0]
      Starting database migration process
info: FWH.Location.Api.Data.DatabaseMigrationService[0]
      Database migration process completed successfully
```

### Alternative: Direct Connection String

If Railway template variables aren't working, you can set the connection string directly:

1. Go to PostgreSQL service → **Variables** tab
2. Copy the `DATABASE_URL` value
3. Go to Location API service → **Variables** tab
4. Set `ConnectionStrings__funwashad` to the copied `DATABASE_URL` value directly (without `${{...}}`)

**Note:** This approach won't automatically update if the database URL changes, but it will work immediately.

### Environment Variables Checklist

For Location API service, ensure these are set:

```bash
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080
ConnectionStrings__funwashad=${{Postgres.DATABASE_URL}}  # ← Verify service name matches!
LocationService__DefaultRadiusMeters=1000
LocationService__MaxRadiusMeters=10000
LocationService__UserAgent=FunWasHad-Staging/1.0
LocationService__OverpassApiUrl=https://overpass-api.de/api/interpreter
```

### Related Documentation

- [Railway Staging Setup Guide](./railway-staging-setup.md)
- [Railway Environment Variables](https://docs.railway.app/develop/variables)
