# User Secrets Setup for Railway PostgreSQL

## Overview

Railway PostgreSQL credentials are stored in .NET User Secrets for security. This keeps sensitive data out of source control while providing seamless local development.

## Quick Setup

### Windows (PowerShell)
```powershell
.\setup-user-secrets.ps1
```

### macOS/Linux (Bash)
```bash
chmod +x setup-user-secrets.sh
./setup-user-secrets.sh
```

## Manual Setup

If you prefer to set up User Secrets manually:

### 1. AppHost
```bash
cd src/FWH.AppHost
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:funwashad" "Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "ConnectionStrings:marketing" "Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true"
```

### 2. Location API
```bash
cd ../FWH.Location.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:funwashad" "Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true"
```

### 3. Marketing API
```bash
cd ../FWH.MarketingApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:marketing" "Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true"
```

## Verify Configuration

View configured secrets for any project:
```bash
cd src/FWH.AppHost
dotnet user-secrets list
```

Expected output:
```
ConnectionStrings:funwashad = Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true
ConnectionStrings:marketing = Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true
```

## Where Are Secrets Stored?

**Windows:**
```
%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json
```

**macOS/Linux:**
```
~/.microsoft/usersecrets/<user-secrets-id>/secrets.json
```

Each project has its own `<user-secrets-id>` defined in the `.csproj` file.

## How It Works

1. **Development Priority:** User Secrets override `appsettings.Development.json`
2. **Not in Git:** Secrets are stored outside the project directory
3. **Per-User:** Each developer has their own secrets on their machine
4. **IDE Support:** Visual Studio and Rider have built-in User Secrets editors

## Managing Secrets

### Update a Secret
```bash
cd src/FWH.AppHost
dotnet user-secrets set "ConnectionStrings:funwashad" "new-connection-string"
```

### Remove a Secret
```bash
dotnet user-secrets remove "ConnectionStrings:funwashad"
```

### Clear All Secrets
```bash
dotnet user-secrets clear
```

## Visual Studio Integration

**Right-click project** → **Manage User Secrets**

This opens the `secrets.json` file in Visual Studio where you can edit secrets directly:

```json
{
  "ConnectionStrings": {
    "funwashad": "Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true",
    "marketing": "Host=shortline.proxy.rlwy.net;Port=41493;Database=railway;Username=postgres;Password=RuxJLfQvuyRdRDosBZzwIDxIJxbuoRDP;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

## Troubleshooting

### "Connection string not found"
1. Verify secrets are set: `dotnet user-secrets list`
2. Ensure you're in Development environment
3. Check UserSecretsId in `.csproj` file

### "User secrets ID not found"
Run `dotnet user-secrets init` to create the UserSecretsId

### Secrets not loading
- Restart Visual Studio/IDE
- Verify `ASPNETCORE_ENVIRONMENT=Development`
- Check that `Microsoft.Extensions.Configuration.UserSecrets` package is referenced

## Team Setup

Each team member should:
1. Pull the latest code
2. Run the setup script (`setup-user-secrets.ps1` or `setup-user-secrets.sh`)
3. Verify with `dotnet user-secrets list`

## Updating Railway Credentials

If Railway credentials change:
1. Update the connection string in the setup script
2. Notify team members to re-run the script
3. Or manually update secrets with `dotnet user-secrets set`

## Security Benefits

✅ **Not in Git** - Credentials never committed to repository  
✅ **Per-Developer** - Each developer has their own secrets  
✅ **Local Only** - Secrets stored on local machine  
✅ **Development Only** - User Secrets only work in Development environment  
✅ **No Conflicts** - Team members can use different databases/credentials  

## Production/Staging

User Secrets are **Development-only**. Production and Staging use:
- **Railway:** Environment variables
- **Azure:** Key Vault
- **AWS:** Secrets Manager
- **Environment Variables:** Set by hosting platform
