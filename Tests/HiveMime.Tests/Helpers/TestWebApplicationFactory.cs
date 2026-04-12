using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HiveMime.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TestWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.RemoveAll<DbContextOptions<HiveMimeContext>>();

            // Register DbContext with Testcontainers connection string
            services.AddDbContextFactory<HiveMimeContext>(options =>
                options.UseNpgsql(_connectionString), ServiceLifetime.Scoped);
        });
    }
}
