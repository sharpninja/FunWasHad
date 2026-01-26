using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>Root service provider from the built test server. Use CreateScope() for scoped services.</summary>
    public IServiceProvider Services => Server.Host.Services;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Set environment to prevent migrations from running
        builder.UseEnvironment("Test");

        // Configure connection string BEFORE Program.cs runs
        // This prevents Aspire from failing during AddNpgsqlDbContext validation
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:funwashad", "Host=localhost;Database=test;Username=test;Password=test" },
                { "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:LocationDbContext:ConnectionString", "Host=localhost;Database=test;Username=test;Password=test" }
            });
        });

        // Override services AFTER Program.cs has run
        // This allows us to remove Aspire's pooled DbContext and replace with in-memory
        builder.ConfigureServices(services =>
        {
            // Remove ALL existing DbContext registrations including pooling
            // Use RemoveAll to ensure we get all registrations
            services.RemoveAll<DbContextOptions<LocationDbContext>>();
            services.RemoveAll<LocationDbContext>();

            // Remove pooling services by type
            services.RemoveAll(typeof(IDbContextPool<>));
            services.RemoveAll(typeof(IDbContextPool<LocationDbContext>));
            services.RemoveAll(typeof(IScopedDbContextLease<>));
            services.RemoveAll(typeof(IScopedDbContextLease<LocationDbContext>));

            // Also remove any remaining by checking all descriptors
            var allDescriptors = services.ToList();
            foreach (var descriptor in allDescriptors)
            {
                if (descriptor.ServiceType.IsGenericType)
                {
                    var genericTypeDef = descriptor.ServiceType.GetGenericTypeDefinition();
                    var genericArgs = descriptor.ServiceType.GetGenericArguments();

                    if (genericArgs.Length > 0 && genericArgs[0] == typeof(LocationDbContext))
                    {
                        if (genericTypeDef == typeof(IDbContextPool<>) ||
                            genericTypeDef == typeof(IScopedDbContextLease<>))
                        {
                            services.Remove(descriptor);
                        }
                    }
                }
            }

            // Register DbContext WITHOUT pooling for tests using in-memory database
            // Use a fixed database name so all scopes share the same in-memory database
            services.AddDbContext<LocationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("location-tests-shared");
                options.EnableSensitiveDataLogging();
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

            services.RemoveAll<ILocationService>();
            services.AddSingleton(LocationServiceSubstitute);

            // Ensure database schema is created when host starts
            services.AddSingleton<IHostedService, EnsureDbCreatedHostedService>();
        });
    }

    private sealed class EnsureDbCreatedHostedService : IHostedService
    {
        private readonly IServiceProvider _services;

        public EnsureDbCreatedHostedService(IServiceProvider services)
        {
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();
            db.Database.EnsureCreated();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
