using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json.Serialization;
using Npgsql;

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
            var npgsqlBuilder = new NpgsqlDataSourceBuilder(_connectionString);
            npgsqlBuilder.EnableDynamicJson();
            npgsqlBuilder.ConfigureJsonOptions(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new VoteQueryConverter()
                }
            });

            services.AddDbContextFactory<HiveMimeContext>(options =>
                options.UseNpgsql(npgsqlBuilder.Build()), ServiceLifetime.Scoped);

            // Add a mock IConfiguration with dummy values.
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["R2:AccessKey"] = "test-access-key",
                ["R2:SecretKey"] = "test-secret-key",
                ["R2:S3API"] = "https://localhost"
            });

            services.RemoveAll<IConfiguration>();
            services.AddSingleton<IConfiguration>(builder.Build());
        });
    }
}
