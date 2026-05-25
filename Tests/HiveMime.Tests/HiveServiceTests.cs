using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class HiveServiceTests : IntegrationTest
{
    private HiveService _service;
    private Hive? _defaultHive;

    public HiveServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<HiveService>();
    }

    [Fact]
    public async Task JoinHive_WithNewFollow_JoinsUser()
    {
        // Arrange
        var user = new User { Username = "newuser", Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var hive = Context.Hives.First();
        Context.ChangeTracker.Clear();

        // Act
        await _service.JoinHiveAsync(user.Id, hive.Id);
        bool isFollower = await Context.Hives.Where(h => h.Id == hive.Id)
            .SelectMany(h => h.Users)
            .AnyAsync(u => u.Id == user.Id);

        // Assert
        Assert.True(isFollower);
    }

    [Fact]
    public async Task LeaveHive_WithExistingFollow_RemovesUser()
    {
        // Arrange
        var hive = Context.Hives.First();
        var user = Context.Users.First();
        Context.ChangeTracker.Clear();

        // Act
        await _service.LeaveHiveAsync(user.Id, hive.Id);
        bool isFollower = await Context.Hives.Where(h => h.Id == hive.Id)
            .SelectMany(h => h.Users)
            .AnyAsync(u => u.Id == user.Id);

        // Assert
        Assert.False(isFollower);
    }

    [Fact]
    public async Task GetFollowedHives_WithExistingFollow_ReturnsExpected()
    {
        // Arrange
        var user = Context.Users.First();
        Context.ChangeTracker.Clear();

        // Act
        var hives = await _service.GetJoinedHivesAsync(user.Id);

        // Assert
        Assert.Equal(_defaultHive!.Id, hives.Single().Id);
    }

    [Fact]
    public async Task GetHive_WithValidId_ReturnsHive()
    {
        // Act
        var result = await _service.GetHiveAsync(_defaultHive!.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_defaultHive.Id, result.Id);
        Assert.Equal("Default Hive", result.Name);
    }

    [Fact]
    public async Task BrowseHives_WithHives_ReturnsHives()
    {
        // Act
        var result = await _service.BrowseHivesAsync(new HivePaginationDto { PageSize = 20 });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(_defaultHive!.Id, result.Items[0].Id);
        Assert.Equal("Default Hive", result.Items[0].Name);
    }

    [Fact]
    public async Task CreateHive_ValidHive_AddsToDatabase()
    {
        // Arrange
        var user = Context.Users.First();
        var hiveDto = new CreateHiveDto
        {
            Name = "New Hive",
            Description = "New hive description"
        };
        var service = _service;

        // Act
        var result = await service.CreateHiveAsync(user.Id, hiveDto);
        var hive = Context.Hives.FirstOrDefault(h => h.Name == "New Hive");

        // Assert
        Assert.NotNull(hive);
        Assert.Equal("New Hive", hive.Name);
        Assert.Equal("New hive description", hive.Description);
        Assert.Equal(user.Id, hive.CreatorId);
    }

    [Fact]
    public async Task CreateHive_InvalidName_ThrowsException()
    {
        // Arrange
        var user = Context.Users.First();
        var service = _service;
        var hiveDto = new CreateHiveDto
        {
            Name = "ab",
            Description = "Too short name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateHiveAsync(user.Id, hiveDto));
    }

    [Fact]
    public async Task CreateHive_DuplicateName_ThrowsException()
    {
        // Arrange
        var user = Context.Users.First();
        var service = _service;
        var hiveDto = new CreateHiveDto
        {
            Name = "Default Hive",
            Description = "Duplicate name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateHiveAsync(user.Id, hiveDto));
    }

    [Fact]
    public async Task BrowseHivesAsync_Pagination_WorksWithFilterAndOrder()
    {
        // Arrange
        var hive1 = new Hive { Name = "AlphaHive", Description = "desc", Creator = Context.Users.First() };
        var hive2 = new Hive { Name = "BetaHive", Description = "desc", Creator = Context.Users.First() };
        Context.Hives.AddRange(hive1, hive2);
        await Context.SaveChangesAsync();
        var pagination = new HivePaginationDto { Filter = "Alpha", OrderBy = HiveOrderBy.New, PageSize = 1 };

        // Act
        var hives = await _service.BrowseHivesAsync(pagination);

        // Assert
        Assert.Single(hives.Items);
        Assert.Contains("Alpha", hives.Items[0].Name);
    }

    [Fact]
    public async Task AddModeratorAsync_ValidRequest_AddsRequestedModerator()
    {
        // Arrange
        var creator = new User { Username = "creator-mod", Settings = new() };
        var requestedModerator = new User { Username = "requested-mod", Settings = new() };
        Context.Users.AddRange(creator, requestedModerator);
        await Context.SaveChangesAsync();

        var hive = new Hive { Name = "Mod Hive", Description = "desc", Creator = creator, Moderators = [], Users = [] };
        Context.Hives.Add(hive);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Act
        await _service.AddModeratorAsync(creator.Id, requestedModerator.Id, hive.Id);
        var moderators = await Context.Hives.Where(h => h.Id == hive.Id).SelectMany(h => h.Moderators).ToListAsync();

        // Assert
        Assert.Contains(moderators, m => m.Id == requestedModerator.Id);
    }

    [Fact]
    public async Task AddModeratorAsync_UnauthorizedUser_ThrowsException()
    {
        // Arrange
        var creator = new User { Username = "creator-noauth", Settings = new() };
        var outsider = new User { Username = "outsider-noauth", Settings = new() };
        var requestedModerator = new User { Username = "requested-noauth", Settings = new() };
        Context.Users.AddRange(creator, outsider, requestedModerator);
        await Context.SaveChangesAsync();

        var hive = new Hive { Name = "No Auth Hive", Description = "desc", Creator = creator, Moderators = [], Users = [] };
        Context.Hives.Add(hive);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.AddModeratorAsync(outsider.Id, requestedModerator.Id, hive.Id));
    }

    [Fact]
    public async Task RemoveModeratorAsync_ValidRequest_RemovesModerator()
    {
        // Arrange
        var creator = new User { Username = "creator-remove", Settings = new() };
        var moderator = new User { Username = "moderator-remove", Settings = new() };
        Context.Users.AddRange(creator, moderator);
        await Context.SaveChangesAsync();

        var hive = new Hive
        {
            Name = "Remove Mod Hive",
            Description = "desc",
            Creator = creator,
            Moderators = [moderator],
            Users = []
        };
        Context.Hives.Add(hive);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Act
        await _service.RemoveModeratorAsync(creator.Id, moderator.Id, hive.Id);
        var moderators = await Context.Hives.Where(h => h.Id == hive.Id).SelectMany(h => h.Moderators).ToListAsync();

        // Assert
        Assert.DoesNotContain(moderators, m => m.Id == moderator.Id);
    }

    [Fact]
    public async Task RemoveModeratorAsync_UnauthorizedUser_ThrowsException()
    {
        // Arrange
        var creator = new User { Username = "creator-remove-noauth", Settings = new() };
        var moderator = new User { Username = "moderator-remove-noauth", Settings = new() };
        var outsider = new User { Username = "outsider-remove-noauth", Settings = new() };
        Context.Users.AddRange(creator, moderator, outsider);
        await Context.SaveChangesAsync();

        var hive = new Hive
        {
            Name = "Remove No Auth Hive",
            Description = "desc",
            Creator = creator,
            Moderators = [moderator],
            Users = []
        };
        Context.Hives.Add(hive);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RemoveModeratorAsync(outsider.Id, moderator.Id, hive.Id));
    }

    protected override void SeedDatabase()
    {
        var user = new User { Username = "defaultuser", Settings = new() };
        Context.Users.Add(user);

        _defaultHive = new Hive
        {
            Name = "Default Hive",
            Description = "This is a default hive.",
            Creator = user,
            Posts = [],
            Users = [new() { User = user, IsApproved = true }]
        };
        Context.Hives.Add(_defaultHive);
    }
}
