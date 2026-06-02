using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class HiveServiceTests : IntegrationTest
{
    private HiveService _service;
    private User? _defaultUser;
    private Hive? _defaultHive;
    private HiveUser? _defaultMembership;

    public HiveServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<HiveService>();
    }

    [Fact]
    public async Task JoinHive_WithNewUser_CreatesApprovedMembershipWhenApprovalNotRequired()
    {
        // Arrange
        var user = new User { Username = "newuser", Settings = new() };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Act
        var joined = await _service.JoinHiveAsync(user.Id, _defaultHive!.Id);
        var hiveUser = await Context.HiveUsers.FirstOrDefaultAsync(h => h.Id == joined.Id);

        // Assert
        Assert.NotNull(hiveUser);
        Assert.Equal(ApprovalStatus.Approved, hiveUser!.ApprovalStatus);
        Assert.Equal(MemberRole.Follower, hiveUser.Role);
    }

    [Fact]
    public async Task LeaveHive_WithFollowerMembership_RemovesUserFromHive()
    {
        // Arrange
        var follower = new User { Username = "leaving-follower", Settings = new() };
        Context.Users.Add(follower);
        await Context.SaveChangesAsync();

        var followerMembership = new HiveUser
        {
            HiveId = _defaultHive!.Id,
            UserId = follower.Id,
            Role = MemberRole.Follower,
            ApprovalStatus = ApprovalStatus.Approved
        };
        Context.HiveUsers.Add(followerMembership);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        // Act
        await _service.LeaveHiveAsync(follower.Id, followerMembership.Id);
        var exists = await Context.HiveUsers.AnyAsync(h => h.Id == followerMembership.Id);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task LeaveHive_WithOtherUsersMembership_ThrowsValidation()
    {
        // Arrange
        var otherUser = new User { Username = "other-user", Settings = new() };
        Context.Users.Add(otherUser);
        await Context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.LeaveHiveAsync(otherUser.Id, _defaultMembership!.Id));
    }

    [Fact]
    public async Task LeaveHive_WithCreatorMembership_ThrowsValidation()
    {
        // Arrange
        Context.ChangeTracker.Clear();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.LeaveHiveAsync(_defaultUser!.Id, _defaultMembership!.Id));
    }

    [Fact]
    public async Task LeaveHive_WithRejectedMembership_RemovesUserFromHive()
    {
        // Arrange
        var rejectedUser = new User { Username = "rejected-user", Settings = new() };
        Context.Users.Add(rejectedUser);
        await Context.SaveChangesAsync();

        var rejectedMembership = new HiveUser
        {
            HiveId = _defaultHive!.Id,
            UserId = rejectedUser.Id,
            Role = MemberRole.Follower,
            ApprovalStatus = ApprovalStatus.Rejected
        };
        Context.HiveUsers.Add(rejectedMembership);
        await Context.SaveChangesAsync();

        // Act
        await _service.LeaveHiveAsync(rejectedUser.Id, rejectedMembership.Id);
        var exists = await Context.HiveUsers.AnyAsync(h => h.Id == rejectedMembership.Id);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetJoinedHives_WithExistingMembership_ReturnsExpectedHive()
    {
        // Arrange
        Context.ChangeTracker.Clear();

        // Act
        var hives = await _service.GetJoinedHivesAsync(_defaultUser!.Id);

        // Assert
        Assert.Single(hives);
        Assert.Equal(_defaultHive!.Id, hives[0].Hive.Id);
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
    public async Task CreateHive_ValidHive_AddsCreatorMembership()
    {
        // Arrange
        var hiveDto = new CreateHiveDto
        {
            Name = "New Hive",
            Description = "New hive description"
        };

        // Act
        var result = await _service.CreateHiveAsync(_defaultUser!.Id, hiveDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MemberRole.Creator, result!.Role);
        Assert.Equal(ApprovalStatus.Approved, result.ApprovalStatus);
    }

    [Fact]
    public async Task CreateHive_InvalidName_ThrowsException()
    {
        // Arrange
        var hiveDto = new CreateHiveDto
        {
            Name = "ab",
            Description = "Too short name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateHiveAsync(_defaultUser!.Id, hiveDto));
    }

    [Fact]
    public async Task CreateHive_DuplicateName_ThrowsException()
    {
        // Arrange
        var hiveDto = new CreateHiveDto
        {
            Name = "Default Hive",
            Description = "Duplicate name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateHiveAsync(_defaultUser!.Id, hiveDto));
    }

    [Fact]
    public async Task CreateHive_UnverifiedUser_ThrowsUnauthorized()
    {
        // Arrange
        var unverifiedUser = new User { Username = "unverified-user", IsVerified = false, Settings = new() };
        Context.Users.Add(unverifiedUser);
        await Context.SaveChangesAsync();

        var hiveDto = new CreateHiveDto
        {
            Name = "Unverified Hive",
            Description = "Should not be created"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateHiveAsync(unverifiedUser.Id, hiveDto));
    }

    [Fact]
    public async Task BrowseHivesAsync_Pagination_WorksWithFilterAndOrder()
    {
        // Arrange
        var hive1 = new Hive { Name = "AlphaHive", Description = "desc", Users = [new() { User = _defaultUser!, Role = MemberRole.Creator, ApprovalStatus = ApprovalStatus.Approved }] };
        var hive2 = new Hive { Name = "BetaHive", Description = "desc", Users = [new() { User = _defaultUser!, Role = MemberRole.Creator, ApprovalStatus = ApprovalStatus.Approved }] };
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
    public async Task ModifyHiveUserAsync_CreatorPromotesFollower_UpdatesRoleAndApproval()
    {
        // Arrange
        var follower = new User { Username = "member-user", Settings = new() };
        Context.Users.Add(follower);
        await Context.SaveChangesAsync();

        var membership = new HiveUser
        {
            HiveId = _defaultHive!.Id,
            UserId = follower.Id,
            Role = MemberRole.Follower,
            ApprovalStatus = ApprovalStatus.Approved
        };
        Context.HiveUsers.Add(membership);
        await Context.SaveChangesAsync();

        // Act
        await _service.ModifyHiveUserAsync(_defaultUser!.Id, membership.Id, MemberRole.Moderator, ApprovalStatus.Approved);
        var updated = await Context.HiveUsers.FirstAsync(h => h.Id == membership.Id);

        // Assert
        Assert.Equal(MemberRole.Moderator, updated.Role);
        Assert.Equal(ApprovalStatus.Approved, updated.ApprovalStatus);
    }

    [Fact]
    public async Task ModifyHiveUserAsync_ModeratorCannotAssignEqualRole_ThrowsUnauthorized()
    {
        // Arrange
        var moderator = new User { Username = "mod-user", Settings = new() };
        var member = new User { Username = "member-user-2", Settings = new() };
        Context.Users.AddRange(moderator, member);
        await Context.SaveChangesAsync();

        var moderatorMembership = new HiveUser
        {
            HiveId = _defaultHive!.Id,
            UserId = moderator.Id,
            Role = MemberRole.Moderator,
            ApprovalStatus = ApprovalStatus.Approved
        };
        var targetMembership = new HiveUser
        {
            HiveId = _defaultHive.Id,
            UserId = member.Id,
            Role = MemberRole.Follower,
            ApprovalStatus = ApprovalStatus.Pending
        };

        Context.HiveUsers.AddRange(moderatorMembership, targetMembership);
        await Context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.ModifyHiveUserAsync(moderator.Id, targetMembership.Id, MemberRole.Moderator, ApprovalStatus.Approved));
    }

    [Fact]
    public async Task GetUsersAsync_ModeratorCanViewPendingUsers()
    {
        // Arrange
        var moderator = new User { Username = "viewer-mod", Settings = new() };
        var pendingUser = new User { Username = "pending-user", Settings = new() };
        Context.Users.AddRange(moderator, pendingUser);
        await Context.SaveChangesAsync();

        Context.HiveUsers.AddRange(
            new HiveUser { HiveId = _defaultHive!.Id, UserId = moderator.Id, Role = MemberRole.Moderator, ApprovalStatus = ApprovalStatus.Approved },
            new HiveUser { HiveId = _defaultHive.Id, UserId = pendingUser.Id, Role = MemberRole.Follower, ApprovalStatus = ApprovalStatus.Pending }
        );
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.GetUsersAsync(moderator.Id, _defaultHive.Id, ApprovalStatus.Pending, new HiveUserPaginationDto { PageSize = 20 });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(pendingUser.Id, result.Items[0].User.Id);
    }

    [Fact]
    public async Task GetUsersAsync_Outsider_ThrowsNotFound()
    {
        // Arrange
        var outsider = new User { Username = "outsider", Settings = new() };
        Context.Users.Add(outsider);
        await Context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetUsersAsync(outsider.Id, _defaultHive!.Id, ApprovalStatus.Approved, new HiveUserPaginationDto { PageSize = 20 }));
    }

    [Fact]
    public async Task BanHiveUserAsync_ModeratorBansFollower_SetsBannedStatus()
    {
        // Arrange
        var moderator = new User { Username = "listed-mod", Settings = new() };
        var follower = new User { Username = "listed-follower", Settings = new() };
        Context.Users.AddRange(moderator, follower);
        await Context.SaveChangesAsync();

        Context.HiveUsers.AddRange(
            new HiveUser
            {
                HiveId = _defaultHive!.Id,
                UserId = moderator.Id,
                Role = MemberRole.Moderator,
                ApprovalStatus = ApprovalStatus.Approved
            },
            new HiveUser
            {
                HiveId = _defaultHive.Id,
                UserId = follower.Id,
                Role = MemberRole.Follower,
                ApprovalStatus = ApprovalStatus.Approved
            }
        );
        await Context.SaveChangesAsync();

        // Act
        await _service.BanHiveUserAsync(moderator.Id, follower.Id, _defaultHive.Id);
        var banned = await Context.HiveUsers.FirstAsync(h => h.HiveId == _defaultHive.Id && h.UserId == follower.Id);

        // Assert
        Assert.Equal(ApprovalStatus.Banned, banned.ApprovalStatus);
        Assert.Equal(MemberRole.Follower, banned.Role);
    }

    protected override void SeedDatabase()
    {
        _defaultUser = new User { Username = "defaultuser", IsVerified = true, Settings = new() };
        Context.Users.Add(_defaultUser);

        _defaultHive = new Hive
        {
            Name = "Default Hive",
            Description = "This is a default hive.",
            Posts = [],
            Settings = new HiveSettings
            {
                JoinRequiresApproval = false,
                PostRequiresApproval = false,
                MinRoleToPost = MemberRole.Guest
            },
            Users = [new() { User = _defaultUser, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Creator }]
        };
        Context.Hives.Add(_defaultHive);
        Context.SaveChanges();

        _defaultMembership = Context.HiveUsers.First(h => h.HiveId == _defaultHive.Id && h.UserId == _defaultUser.Id);
    }
}