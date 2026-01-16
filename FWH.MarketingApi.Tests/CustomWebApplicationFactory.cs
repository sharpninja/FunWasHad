using FWH.MarketingApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Custom web application factory for Marketing API integration tests.
/// Implements TR-TEST-002: Integration Tests - API endpoints.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace database with in-memory for testing
            services.RemoveAll<DbContextOptions<MarketingDbContext>>();
            services.RemoveAll<MarketingDbContext>();
            services.AddDbContext<MarketingDbContext>(options =>
                options.UseInMemoryDatabase($"marketing-tests-{Guid.NewGuid()}"));
        });

        return base.CreateHost(builder);
    }
}
