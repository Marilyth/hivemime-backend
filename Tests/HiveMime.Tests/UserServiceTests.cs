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
        await Assert.ThrowsAnyAsync<Exception>(() => _service.GetUserDetailsAsync(999));
    }

    [Fact]
    public async Task CreateUserAsync_ValidClaims_CreatesUser()
    {
        // Arrange
        var claims = new List<Claim> { new("user_id", "test-uid"), new("firebase", "{\"sign_in_provider\":\"test\"}") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.CreateOrLoginUserAsync(principal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-uid", Context.Users.First().FirebaseId);
    }

    [Fact]
    public async Task CreateUserAsync_MissingUserIdClaim_ThrowsException()
    {
        // Arrange
        var claims = new List<Claim>(); // No user_id claim
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateOrLoginUserAsync(principal));
    }

    [Fact]
    public async Task CreateUserAsync_AnonymousUser_SetsFlags()
    {
        // Arrange
        var claims = new List<Claim> { new("user_id", "uid"), new("firebase", "{\"sign_in_provider\":\"anonymous\"}") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.CreateOrLoginUserAsync(principal);
        var user = Context.Users.First(u => u.FirebaseId == "uid");

        // Assert
        Assert.True(user.IsAnonymous);
        Assert.False(user.IsVerified);
        Assert.True(user.Username.StartsWith("guest_"));
    }

    [Fact]
    public async Task CreateUserAsync_UnverifiedEmailUser_SetsFlags()
    {
        // Arrange
        var claims = new List<Claim> {
            new("user_id", "uid"),
            new("firebase", "{\"sign_in_provider\":\"password\"}"),
            new("email", "test@test.com"),
            new("email_verified", "false")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.CreateOrLoginUserAsync(principal);
        var user = Context.Users.First(u => u.FirebaseId == "uid");

        // Assert
        Assert.False(user.IsAnonymous);
        Assert.False(user.IsVerified);
        Assert.True(user.Username.Equals("test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LoginUserAsync_PreviouslyAnonymousUser_UpdatesFlags()
    {
        // Arrange
        Context.Users.Add(new() { Username = "guest_123123", IsAnonymous = true, FirebaseId = "existing-uid", Settings = new() });
        await Context.SaveChangesAsync();

        var claims = new List<Claim> {
            new("user_id", "existing-uid"),
            new("firebase", "{\"sign_in_provider\":\"password\"}"),
            new("email", "test@test.com"),
            new("email_verified", "true")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.CreateOrLoginUserAsync(principal);
        var user = Context.Users.First(u => u.FirebaseId == "existing-uid");

        // Assert
        Assert.False(user.IsAnonymous);
        Assert.True(user.IsVerified);
        Assert.True(user.Username.Equals("test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmailUser_AddsRandomString()
    {
        // Arrange
        Context.Users.Add(new() { Username = "test", Settings = new() });
        await Context.SaveChangesAsync();

        var claims = new List<Claim> {
            new("user_id", "uid"),
            new("firebase", "{\"sign_in_provider\":\"password\"}"),
            new("email", "test@test.com"),
            new("email_verified", "true")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _service.CreateOrLoginUserAsync(principal);
        var user = Context.Users.First(u => u.FirebaseId == "uid");

        // Assert
        Assert.True(user.Username.StartsWith("test_", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MergeAccountsAsync_ValidUserIds_MergesAccounts()
    {
        // ToDo.
    }
}
