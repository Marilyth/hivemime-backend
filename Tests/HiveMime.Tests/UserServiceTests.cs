using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class UserServiceTests : IntegrationTest
{
    private IUserService _service;

    public UserServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<IUserService>();
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
    public async Task Login_ExistingUser_ReturnsToken()
    {
        // Arrange
        var user = new User { Username = "testuser", Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Act
        var result = _service.Login("testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }
}
