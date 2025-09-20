using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.EntityFrameworkCore;

namespace HiveMime.Tests;

public class UserServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private HiveMimeContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HiveMimeContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        var context = new HiveMimeContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }

    private IConfiguration CreateMockConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Key", "this is my custom Secret key for authnetication"},
            {"Jwt:Issuer", "test.com"},
            {"Jwt:Audience", "test.com"},
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task Login_NullUser_CreatesAnonymousUser()
    {
        // Arrange
        await using var context = CreateContext();
        var configuration = CreateMockConfiguration();
        var service = new UserService(context, configuration);

        // Act
        var result = service.Login(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Anonymous", result.Username);
        Assert.False(string.IsNullOrEmpty(result.Token));

        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "Anonymous");
        Assert.NotNull(user);
    }

    [Fact]
    public async Task Login_ExistingUser_ReturnsToken()
    {
        // Arrange
        await using var context = CreateContext();
        var user = new User { Username = "testuser" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var configuration = CreateMockConfiguration();
        var service = new UserService(context, configuration);

        // Act
        var result = service.Login(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }
}
