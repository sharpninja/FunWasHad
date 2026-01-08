using FWH.Common.Location;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var locationOptions = builder.Configuration.GetSection("LocationService").Get<LocationServiceOptions>() ?? new();

builder.Services.AddLocationServicesWithInMemoryConfig(options =>
{
    options.DefaultRadiusMeters = locationOptions.DefaultRadiusMeters;
    options.MaxRadiusMeters = locationOptions.MaxRadiusMeters;
    options.MinRadiusMeters = locationOptions.MinRadiusMeters;
    options.TimeoutSeconds = locationOptions.TimeoutSeconds;
    options.UserAgent = locationOptions.UserAgent;
    options.OverpassApiUrl = locationOptions.OverpassApiUrl;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
