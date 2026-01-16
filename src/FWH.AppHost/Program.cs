var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database with persistent local storage
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("funwashad-postgres-data")  // Use local volume for persistence
    .WithPgAdmin();

// Add databases
var locationDb = postgres.AddDatabase("funwashad");
var marketingDb = postgres.AddDatabase("marketing");

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

// Note: Mobile app can connect to the APIs at:
// Location API:
// - Android emulator: http://10.0.2.2:4748
// - Desktop/iOS: https://localhost:4747
// Marketing API:
// - Android emulator: http://10.0.2.2:4750
// - Desktop/iOS: https://localhost:4749
// - Physical devices: Set environment variables to your machine's IP
// PostgreSQL data is persisted in Docker volume: funwashad-postgres-data

builder.Build().Run();
