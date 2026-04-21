using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
        var claims = new List<Claim> { new("user_id", "test-uid"), new("firebase", "{\"sign_in_provider\":\"test\", \"identities\":{}}") };
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
        var claims = new List<Claim> { new("user_id", "uid"), new("firebase", "{\"sign_in_provider\":\"anonymous\", \"identities\":{}}") };
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
            new("firebase", "{\"sign_in_provider\":\"password\", \"identities\":{}}"),
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
            new("firebase", "{\"sign_in_provider\":\"password\", \"identities\":{}}"),
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
            new("firebase", "{\"sign_in_provider\":\"password\", \"identities\":{}}"),
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
        // Arrange
        var currentUser = new User { Username = "current", Settings = new(), FollowedHives = new List<Hive>() };
        var previousUser = new User { Username = "previous", Settings = new(), FollowedHives = new List<Hive>() };
        Context.Users.AddRange(currentUser, previousUser);
        await Context.SaveChangesAsync();

        // Reload users to get tracked entities with IDs
        currentUser = Context.Users.First(u => u.Username == "current");
        previousUser = Context.Users.First(u => u.Username == "previous");

        // Create a hive and have previousUser follow it
        var hive = new Hive { Name = "TestHive", Description = "desc", CreatorId = previousUser.Id, Followers = new List<User>() };
        Context.Hives.Add(hive);
        await Context.SaveChangesAsync();

        // Reload hive to get tracked entity with ID
        hive = Context.Hives.First(h => h.Name == "TestHive");

        // Only set navigation from one side
        previousUser.FollowedHives.Add(hive);
        await Context.SaveChangesAsync();

        // Create a post by previousUser
        var post = new Post { CreatorId = previousUser.Id, HiveId = hive.Id, Hotness = 1.0, CommentCount = 0, VoteCount = 0 };
        Context.Posts.Add(post);
        await Context.SaveChangesAsync();

        // Create a comment by previousUser
        var comment = new Comment { Content = "test", UserId = previousUser.Id, PostId = post.Id };
        Context.Comments.Add(comment);
        await Context.SaveChangesAsync();

        // Create a vote by previousUser
        var vote = new PostVote { UserId = previousUser.Id, PostId = post.Id };
        Context.Set<PostVote>().Add(vote);
        await Context.SaveChangesAsync();

        // Detach all tracked entities to avoid concurrency issues
        foreach (var entry in Context.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;

        // Act
        await _service.MergeAccountsAsync(currentUser.Id, previousUser.Id);

        // Assert
        // Previous user should be deleted
        Assert.False(Context.Users.Any(u => u.Id == previousUser.Id));

        // Post, comment, and vote should now belong to currentUser
        Assert.All(Context.Posts, p => Assert.Equal(currentUser.Id, p.CreatorId));
        Assert.All(Context.Comments, c => Assert.Equal(currentUser.Id, c.UserId));
        Assert.All(Context.Set<PostVote>(), v => Assert.Equal(currentUser.Id, v.UserId));

        // Followed hives should be merged
        var refreshedCurrentUser = Context.Users.Include(u => u.FollowedHives).First(u => u.Id == currentUser.Id);
        Assert.Contains(refreshedCurrentUser.FollowedHives, h => h.Id == hive.Id);
    }
}
