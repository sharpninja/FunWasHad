using System.Linq;
using FWH.Common.Location;
using FWH.Location.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace FWH.Location.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public ILocationService LocationServiceSubstitute { get; } = Substitute.For<ILocationService>();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Configure connection string BEFORE base host is created
        // This prevents Aspire from failing during validation
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:funwashad", "Host=localhost;Database=test;Username=test;Password=test" },
                { "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:LocationDbContext:ConnectionString", "Host=localhost;Database=test;Username=test;Password=test" }
            });
        });
        
        // Set environment to prevent migrations from running
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            // Remove Aspire's pooled DbContext registration
            var descriptorOptions = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<LocationDbContext>));
            if (descriptorOptions != null)
                services.Remove(descriptorOptions);

            var descriptorContext = services.SingleOrDefault(d =>
                d.ServiceType == typeof(LocationDbContext));
            if (descriptorContext != null)
                services.Remove(descriptorContext);
            
            // Remove pooling services
            var poolDescriptors = services.Where(d =>
                d.ServiceType.IsGenericType &&
                (d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextPool<>) ||
                 d.ServiceType.GetGenericTypeDefinition() == typeof(IScopedDbContextLease<>)) &&
                d.ServiceType.GetGenericArguments().Length > 0 &&
                d.ServiceType.GetGenericArguments()[0] == typeof(LocationDbContext)
            ).ToList();
            
            foreach (var descriptor in poolDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Register DbContext WITHOUT pooling for tests using in-memory database
            services.AddDbContext<LocationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase($"location-tests-{Guid.NewGuid()}");
                options.EnableSensitiveDataLogging();
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

            services.RemoveAll<ILocationService>();
            services.AddSingleton(LocationServiceSubstitute);
        });
        
        var host = base.CreateHost(builder);
        
        // Ensure database schema is created
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();
            db.Database.EnsureCreated();
        }
        
        return host;
    }
}
