using Testcontainers.PostgreSql;

namespace HiveMime.Tests;

public class DatabaseContainer : IAsyncLifetime
{
    // Docker requires sudo per default. Add yourself to the docker group to avoid this. For example:
    // sudo usermod -aG docker $USER
    // newgrp docker
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithDatabase("hivemime_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
