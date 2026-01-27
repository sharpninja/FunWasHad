var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database with persistent local storage
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("funwashad-postgres-data")  // Use local volume for persistence
    .WithPgAdmin();

// Add databases
var locationDb = postgres.AddDatabase("funwashad");
var marketingDb = postgres.AddDatabase("marketing");

// Add Legal website (MarkdownServer: EULA, Privacy, Contact)
var legalWeb = builder.AddProject<Projects.FWH_Legal_Web>("legalweb")
    .WithHttpEndpoint(port: 5050, name: "http")
    .WithExternalHttpEndpoints();

// Add Location API with PostgreSQL dependency
// Configure with fixed HTTP port 4748 for Android emulator access
var locationApi = builder.AddProject<Projects.FWH_Location_Api>("locationapi")
    .WithReference(locationDb)
    .WithHttpEndpoint(port: 4748, name: "asp-http")
    .WithHttpsEndpoint(port: 4747, name: "asp-https")
    .WithExternalHttpEndpoints();

// Add Marketing API with PostgreSQL dependency
// Configure with fixed HTTP port 4750 for Android emulator access
var marketingApi = builder.AddProject<Projects.FWH_MarketingApi>("marketingapi")
    .WithReference(marketingDb)
    .WithHttpEndpoint(port: 4750, name: "asp-http")
    .WithHttpsEndpoint(port: 4749, name: "asp-https")
    .WithExternalHttpEndpoints();

// Legal website: http://localhost:5050 (EULA, Privacy Policy, Corporate Contact)
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
// PostgreSQL data is persisted in Docker volume: funwashad-postgres-data

builder.Build().Run();
