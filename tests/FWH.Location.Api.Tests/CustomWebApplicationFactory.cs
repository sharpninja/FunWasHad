using System.Linq;
using FWH.Common.Location;
using FWH.Location.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LocationDbContext>>();
            services.RemoveAll<LocationDbContext>();
            services.AddDbContext<LocationDbContext>(options => options.UseInMemoryDatabase($"location-tests-{Guid.NewGuid()}"));

            services.RemoveAll<ILocationService>();
            services.AddSingleton(LocationServiceSubstitute);
        });

        return base.CreateHost(builder);
    }
}
