using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Add Legal website (MarkdownServer: EULA, Privacy, Contact)
var legalWeb = builder.AddProject<Projects.FWH_Legal_Web>("legalweb")
    .WithHttpEndpoint(port: 5052, name: "legalweb-http")
    .WithExternalHttpEndpoints();

// Add Location API without PostgreSQL dependency
// Configure with fixed HTTP port 4748 for Android emulator access
// Note: Location API expects ConnectionStrings:funwashad in appsettings or environment variables
var locationApi = builder.AddProject<Projects.FWH_Location_Api>("locationapi")
    .WithHttpEndpoint(port: 4748, name: "location-http")
    .WithHttpsEndpoint(port: 4747, name: "location-https")
    .WithExternalHttpEndpoints();

// Add Marketing API without PostgreSQL dependency
// Configure with fixed HTTP port 4750 for Android emulator access
// Note: Marketing API expects ConnectionStrings:marketing in appsettings or environment variables
var marketingApi = builder.AddProject<Projects.FWH_MarketingApi>("marketingapi")
    .WithHttpEndpoint(port: 4750, name: "marketing-http")
    .WithHttpsEndpoint(port: 4749, name: "marketing-https")
    .WithExternalHttpEndpoints();

// Legal website: http://localhost:5052 (EULA, Privacy Policy, Corporate Contact)
//
// Note: Mobile app can connect to the APIs at:
// Location API:
// - Android emulator: http://10.0.2.2:4748
// - Physical device: http://<your-ip>:4748 (e.g., http://192.168.1.77:4748)
// - Desktop/iOS: https://localhost:4747 or http://localhost:4748
// Marketing API:
// - Android emulator: http://10.0.2.2:4750
// - Physical device: http://<your-ip>:4750 (e.g., http://192.168.1.77:4750)
// - Desktop/iOS: https://localhost:4749 or http://localhost:4750
//
// In Development mode, APIs listen on 0.0.0.0 (all interfaces) to accept
// connections from Android devices on the local network. The MSBuild target
// in FWH.Mobile.Android automatically detects your IP and configures the app.
//
// PostgreSQL connection strings must be configured in:
// - appsettings.Development.json (local development)
// - appsettings.Staging.json (staging environment)
// - Environment variables (production/Railway)

builder.Build().Run();
