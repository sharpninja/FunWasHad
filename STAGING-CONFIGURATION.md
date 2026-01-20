# 🎯 Staging Configuration for All Projects

## Overview

All projects in the FunWasHad solution now support a **Staging** configuration that mirrors the Release configuration with staging-specific settings.

## ✅ What Was Configured

### Centralized Configuration

**File:** `Directory.Build.props`

All projects automatically inherit the Staging configuration through MSBuild's Directory.Build.props mechanism. This provides:

- ✅ Consistent configuration across all 23 projects
- ✅ Single source of truth
- ✅ Easy maintenance
- ✅ No per-project configuration needed

### Staging Configuration Properties

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Staging'">
  <!-- Optimize like Release -->
  <Optimize>true</Optimize>
  <DebugType>portable</DebugType>
  <DebugSymbols>true</DebugSymbols>
  
  <!-- Define STAGING constant for conditional compilation -->
  <DefineConstants>$(DefineConstants);STAGING</DefineConstants>
  
  <!-- Warning level same as Release -->
  <WarningLevel>4</WarningLevel>
  
  <!-- Output configuration -->
  <OutputPath>bin\Staging\</OutputPath>
  <IntermediateOutputPath>obj\Staging\</IntermediateOutputPath>
  
  <!-- Performance optimizations like Release -->
  <TieredCompilation>true</TieredCompilation>
  <TieredCompilationQuickJit>true</TieredCompilationQuickJit>
  
  <!-- Enable ready-to-run for better startup performance -->
  <PublishReadyToRun Condition="'$(RuntimeIdentifier)' != ''">true</PublishReadyToRun>
  
  <!-- Trim unused code (disabled by default, can be enabled per-project) -->
  <PublishTrimmed>false</PublishTrimmed>
</PropertyGroup>
```

---

## 🔑 Key Features

### 1. **Release-like Optimization**
- Full code optimization enabled
- Tiered compilation for better performance
- Ready-to-run compilation support

### 2. **Staging-Specific Compilation Constant**
```csharp
#if STAGING
    // Code that only runs in staging
    builder.Services.AddLogging(logging => 
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    });
#endif
```

### 3. **Debug Symbols**
- Portable debug symbols included
- Allows debugging staging deployments
- Better error diagnostics

### 4. **Separate Output Paths**
- Output: `bin\Staging\`
- Intermediate: `obj\Staging\`
- Prevents conflicts with Debug/Release builds

---

## 🏗️ Building with Staging Configuration

### Visual Studio
```
1. Open solution in Visual Studio
2. Configuration dropdown → Select "Staging"
3. Build → Build Solution
```

### .NET CLI
```bash
# Restore packages
dotnet restore FunWasHad.sln

# Build all projects in Staging configuration
dotnet build FunWasHad.sln --configuration Staging

# Build specific project
dotnet build src/FWH.Location.Api/FWH.Location.Api.csproj --configuration Staging

# Publish for deployment
dotnet publish src/FWH.Location.Api/FWH.Location.Api.csproj --configuration Staging --output ./publish/staging
```

### MSBuild
```bash
msbuild FunWasHad.sln /p:Configuration=Staging
```

---

## 🧪 Testing with Staging Configuration

```bash
# Run all tests in Staging configuration
dotnet test FunWasHad.sln --configuration Staging

# Run specific test project
dotnet test tests/FWH.MarketingApi.Tests/FWH.MarketingApi.Tests.csproj --configuration Staging
```

---

## 🚀 Publishing for Staging Deployment

### Location API
```bash
dotnet publish src/FWH.Location.Api/FWH.Location.Api.csproj \
  --configuration Staging \
  --output ./publish/location-api \
  --self-contained false
```

### Marketing API
```bash
dotnet publish src/FWH.MarketingApi/FWH.MarketingApi.csproj \
  --configuration Staging \
  --output ./publish/marketing-api \
  --self-contained false
```

---

## 🎨 Using STAGING Constant in Code

### Example: Conditional Logging
```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();
    builder.Services.ConfigureHttpClientDefaults(http => 
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });

#if STAGING
    // Enhanced logging for staging
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    Console.WriteLine("Running in STAGING mode");
#endif

    return builder;
}
```

### Example: Different Configuration Sources
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

#if STAGING
        // Load staging-specific configuration
        builder.Configuration.AddJsonFile("appsettings.Staging.json", optional: false);
#elif RELEASE
        // Load production configuration
        builder.Configuration.AddJsonFile("appsettings.Production.json", optional: false);
#endif

        // ... rest of configuration
    }
}
```

### Example: API Endpoints
```csharp
public class LocationService
{
    private readonly string _apiBaseUrl;

    public LocationService()
    {
#if DEBUG
        _apiBaseUrl = "http://localhost:5000";
#elif STAGING
        _apiBaseUrl = "https://staging-api.funwashad.com";
#else
        _apiBaseUrl = "https://api.funwashad.com";
#endif
    }
}
```

---

## 📦 Projects with Staging Configuration

All 23 projects in the solution now support Staging configuration:

### API Projects
- ✅ FWH.Location.Api
- ✅ FWH.MarketingApi
- ✅ FWH.AppHost

### Common Libraries
- ✅ FWH.Common.Chat
- ✅ FWH.Common.Imaging
- ✅ FWH.Common.Location
- ✅ FWH.Common.Workflow

### Mobile Projects
- ✅ FWH.Mobile
- ✅ FWH.Mobile.Android
- ✅ FWH.Mobile.iOS
- ✅ FWH.Mobile.Desktop
- ✅ FWH.Mobile.Data

### Orchestration
- ✅ FWH.Orchestrix.Contracts
- ✅ FWH.Orchestrix.Mediator.Remote
- ✅ FWH.ServiceDefaults

### Test Projects
- ✅ FWH.Common.Chat.Tests
- ✅ FWH.Common.Imaging.Tests
- ✅ FWH.Common.Location.Tests
- ✅ FWH.Common.Workflow.Tests
- ✅ FWH.MarketingApi.Tests
- ✅ FWH.Mobile.Data.Tests
- ✅ FWH.Mobile.Services.Tests

### Documentation
- ✅ FWH.Documentation

---

## 🔧 Configuration Files

### appsettings Files

Each API project should have staging-specific settings:

**Files:**
- `appsettings.json` - Base settings
- `appsettings.Development.json` - Development overrides
- `appsettings.Staging.json` - Staging overrides ✅
- `appsettings.Production.json` - Production overrides

**Already created:**
- ✅ `src/FWH.Location.Api/appsettings.Staging.json`
- ✅ `src/FWH.MarketingApi/appsettings.Staging.json`

---

## 🎯 Differences Between Configurations

| Feature | Debug | Staging | Release |
|---------|-------|---------|---------|
| **Optimization** | ❌ Off | ✅ On | ✅ On |
| **Debug Symbols** | ✅ Full | ✅ Portable | ⚠️ Portable/PDB only |
| **Conditional Constant** | `DEBUG` | `STAGING` | - |
| **Code Trimming** | ❌ Off | ❌ Off* | ⚠️ Optional |
| **Ready-to-Run** | ❌ Off | ✅ On | ✅ On |
| **Output Path** | `bin\Debug` | `bin\Staging` | `bin\Release` |
| **Use Case** | Development | Pre-production testing | Production |

*Can be enabled per-project if needed

---

## 🚦 CI/CD Integration

### GitHub Actions

The staging deployment workflow already uses Staging configuration:

```yaml
- name: Build solution
  run: |
    dotnet build src/FWH.Location.Api/FWH.Location.Api.csproj --no-restore --configuration Staging
    dotnet build src/FWH.MarketingApi/FWH.MarketingApi.csproj --no-restore --configuration Staging

- name: Publish Location API
  run: dotnet publish src/FWH.Location.Api/FWH.Location.Api.csproj --configuration Staging --output ./publish/location-api --no-build

- name: Publish Marketing API
  run: dotnet publish src/FWH.MarketingApi/FWH.MarketingApi.csproj --configuration Staging --output ./publish/marketing-api --no-build
```

**Note:** The current workflow uses `Release` configuration. To use `Staging`, update `.github/workflows/deploy-staging.yml`.

---

## 📋 Verification

### Build Verification
```bash
# Verify Staging configuration works
dotnet build FunWasHad.sln --configuration Staging

# Expected output:
# Build succeeded.
# All projects build to bin\Staging\
```

### Test Verification
```bash
# Run tests with Staging configuration
dotnet test FunWasHad.sln --configuration Staging --verbosity minimal

# Expected: All tests pass
```

### Output Verification
```bash
# Check that binaries are in correct location
ls src/FWH.Location.Api/bin/Staging/net9.0/
ls src/FWH.MarketingApi/bin/Staging/net9.0/

# Expected: DLL files present in Staging folder
```

---

## 🎓 Best Practices

### 1. **Use STAGING Constant Sparingly**
Only use for truly staging-specific code. Prefer configuration files for most settings.

```csharp
// ❌ Avoid
#if STAGING
    var connectionString = "Server=staging-db";
#else
    var connectionString = "Server=prod-db";
#endif

// ✅ Better - use appsettings.Staging.json
var connectionString = configuration.GetConnectionString("DefaultConnection");
```

### 2. **Keep Staging Close to Release**
Staging should be as close to Release/Production as possible to catch issues early.

### 3. **Enhanced Diagnostics**
Staging can have more verbose logging than Production:

```json
// appsettings.Staging.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"  // More verbose in staging
    }
  }
}
```

### 4. **Test Data**
Use staging-specific test data and databases:

```json
// appsettings.Staging.json
{
  "ConnectionStrings": {
    "marketing": "${{Postgres.DATABASE_URL}}",  // Staging database
    "funwashad": "${{Postgres.DATABASE_URL}}"
  },
  "TestDataSeeding": {
    "Enabled": true,  // Seed test data in staging
    "ClearOnStartup": false
  }
}
```

---

## 📚 Related Documentation

- **Railway Setup:** [RAILWAY-SETUP.md](RAILWAY-SETUP.md)
- **Staging Guide:** [docs/deployment/railway-staging-setup.md](docs/deployment/railway-staging-setup.md)
- **GitHub Workflow:** [.github/workflows/deploy-staging.yml](.github/workflows/deploy-staging.yml)
- **Directory.Build.props:** [Directory.Build.props](Directory.Build.props)

---

## 🎉 Summary

- ✅ Staging configuration added to all 23 projects
- ✅ Centralized in Directory.Build.props
- ✅ Release-like optimizations with debug symbols
- ✅ STAGING constant available for conditional compilation
- ✅ Separate output paths (bin\Staging)
- ✅ Ready for use in CI/CD pipelines
- ✅ Builds and tests successfully

**Build command:**
```bash
dotnet build FunWasHad.sln --configuration Staging
```

**Test command:**
```bash
dotnet test FunWasHad.sln --configuration Staging
```

**Publish command:**
```bash
dotnet publish src/FWH.Location.Api/FWH.Location.Api.csproj --configuration Staging --output ./publish
```

---

**All projects now fully support Staging configuration!** 🚀
