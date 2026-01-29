# Railway PostgreSQL Connection Setup for Local Development

## Overview
The Development environment has been configured to connect to Railway's staging PostgreSQL database instead of running a local PostgreSQL instance.

## Required: Get Railway PostgreSQL Credentials

You need to replace the placeholder values in the following files with actual Railway PostgreSQL credentials:
- `src/FWH.AppHost/appsettings.Development.json`
- `src/FWH.Location.Api/appsettings.Development.json`
- `src/FWH.MarketingApi/appsettings.Development.json`

### Step 1: Get Railway PostgreSQL Connection String

1. **Login to Railway:**
   ```bash
   railway login
   ```

2. **Link to staging project:**
   ```bash
   railway link 9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d
   ```

3. **Get PostgreSQL DATABASE_URL:**
   ```bash
   railway variables --service postgres
   ```
   
   Look for the `DATABASE_URL` variable. It will look like:
   ```
   postgresql://postgres:PASSWORD@HOST:PORT/railway
   ```

### Step 2: Parse Connection String Components

From the DATABASE_URL, extract:
- **HOST**: The hostname after `@` and before `:`
- **PORT**: The port number after the last `:`
- **PASSWORD**: The password between `postgres:` and `@`
- **DATABASE**: Usually `railway` (at the end after `/`)

Example:
```
postgresql://postgres:MyP@ssw0rd123@containers-us-west-123.railway.app:5432/railway
```
Becomes:
- HOST: `containers-us-west-123.railway.app`
- PORT: `5432`
- PASSWORD: `MyP@ssw0rd123`
- DATABASE: `railway`

### Step 3: Update Configuration Files

Replace the placeholders in each file:

#### src/FWH.AppHost/appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "funwashad": "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true",
    "marketing": "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

#### src/FWH.Location.Api/appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "funwashad": "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

#### src/FWH.MarketingApi/appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "marketing": "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### Step 4: Use User Secrets (Recommended for Security)

Instead of storing credentials in `appsettings.Development.json`, use .NET User Secrets:

```bash
# For AppHost
cd src/FWH.AppHost
dotnet user-secrets set "ConnectionStrings:funwashad" "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "ConnectionStrings:marketing" "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

# For Location API
cd ../FWH.Location.Api
dotnet user-secrets set "ConnectionStrings:funwashad" "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

# For Marketing API
cd ../FWH.MarketingApi
dotnet user-secrets set "ConnectionStrings:marketing" "Host=ACTUAL_HOST;Port=ACTUAL_PORT;Database=railway;Username=postgres;Password=ACTUAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

**Benefits of User Secrets:**
- Credentials never committed to Git
- Stored securely in your user profile
- Works seamlessly with local development

## Connection String Parameters Explained

- **SSL Mode=Require**: Railway PostgreSQL requires SSL connections
- **Trust Server Certificate=true**: Accepts Railway's SSL certificate without validation (acceptable for development)
- **Database=railway**: Railway's default database name
- **Username=postgres**: Railway's default PostgreSQL superuser

## Verification

After configuring, test the connection:

```bash
# Run the AppHost
cd src/FWH.AppHost
dotnet run
```

The Location API and Marketing API should start successfully and connect to Railway's PostgreSQL.

## Troubleshooting

### Connection Refused
- Verify host and port are correct
- Check Railway service is running

### Authentication Failed
- Verify password is correct (may contain special characters)
- Ensure you copied the entire password from DATABASE_URL

### SSL Connection Error
- Ensure `SSL Mode=Require;Trust Server Certificate=true` is included
- Railway requires SSL connections

### Can't Find DATABASE_URL
```bash
# List all Railway services
railway status

# Get specific service variables
railway variables --service postgres
```
