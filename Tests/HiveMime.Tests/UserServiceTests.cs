using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Claims;

namespace HiveMime.Tests;

public class UserServiceTests : IntegrationTest
{
    private UserService _service;

    public UserServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<UserService>();
    }

    [Fact]
    public async Task GetUserDetailsAsync_ExistingUser_ReturnsUserDetails()
    {
        // Arrange
        var user = new User { Username = "testuser", Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserDetailsAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task GetUserDetailsAsync_NonExistingUser_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetUserDetailsAsync(999));
    }

    [Fact]
    public async Task CreateUserAsync_ValidClaims_CreatesUser()
    {
        // Arrange
        var claims = new List<Claim> { new("user_id", "test-uid") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.CreateUserAsync(principal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-uid", Context.Users.First().UId);
    }

    [Fact]
    public async Task CreateUserAsync_MissingUserIdClaim_ThrowsException()
    {
        // Arrange
        var claims = new List<Claim>(); // No user_id claim
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateUserAsync(principal));
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUid_ThrowsException()
    {
        // Arrange
        Context.Users.Add(new User { UId = "duplicate-uid", Username = "existinguser", Settings = new() });
        await Context.SaveChangesAsync();

        var claims = new List<Claim> { new("user_id", "duplicate-uid") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateUserAsync(principal));
    }

    [Fact]
    public async Task MergeAccountsAsync_ValidUserIds_MergesAccounts()
    {
        // ToDo.
    }
}
