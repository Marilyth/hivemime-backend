using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class AuthorizationServiceTests : IntegrationTest
{
    private AuthorizationService _service;
    private User? _creator;
    private User? _admin;
    private User? _moderator;
    private User? _follower;
    private User? _outsider;
    private Hive? _hive;
    private HiveUser? _creatorMembership;
    private HiveUser? _followerMembership;
    private HiveUser? _moderatorMembership;
    private HiveUser? _rejectedMembership;
    private Post? _hivePost;
    private Post? _publicPost;
    private Comment? _hiveComment;

    public AuthorizationServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<AuthorizationService>();
    }

    [Fact]
    public async Task VerifyViewHiveUsersAsync_Moderator_DoesNotThrow()
    {
        await _service.VerifyViewHiveUsersAsync(_moderator!.Id, _hive!.Id);
    }

    [Fact]
    public async Task VerifyViewHiveUsersAsync_Outsider_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyViewHiveUsersAsync(_outsider!.Id, _hive!.Id));
    }

    [Fact]
    public async Task VerifyModifyHiveUserAsync_CreatorPromotesFollower_DoesNotThrow()
    {
        await _service.VerifyModifyHiveUserAsync(_creator!.Id, _followerMembership!.Id, MemberRole.Moderator);
    }

    [Fact]
    public async Task VerifyModifyHiveUserAsync_ModeratorPromotesFollowerToModerator_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyModifyHiveUserAsync(_moderator!.Id, _followerMembership!.Id, MemberRole.Moderator));
    }

    [Fact]
    public async Task VerifyLeaveHiveAsync_SelfMembership_DoesNotThrow()
    {
        await _service.VerifyLeaveHiveAsync(_follower!.Id, _followerMembership!.Id);
    }

    [Fact]
    public async Task VerifyLeaveHiveAsync_Outsider_Throws()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.VerifyLeaveHiveAsync(_outsider!.Id, _moderatorMembership!.Id));
    }

    [Fact]
    public async Task VerifyLeaveHiveAsync_CreatorMembership_Throws()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.VerifyLeaveHiveAsync(_creator!.Id, _creatorMembership!.Id));
    }

    [Fact]
    public async Task VerifyLeaveHiveAsync_RejectedMembership_DoesNotThrow()
    {
        await _service.VerifyLeaveHiveAsync(_outsider!.Id, _rejectedMembership!.Id);
    }

    [Fact]
    public async Task VerifyApprovePostAsync_Moderator_DoesNotThrow()
    {
        await _service.VerifyApprovePostAsync(_moderator!.Id, _hivePost!.Id);
    }

    [Fact]
    public async Task VerifyApprovePostAsync_Outsider_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyApprovePostAsync(_outsider!.Id, _hivePost!.Id));
    }

    [Fact]
    public async Task VerifyCreatePostAsync_AnyonePolicyWithEnoughHoney_DoesNotThrow()
    {
        _hive!.Settings.MinRoleToPost = MemberRole.Guest;
        _hive.Settings.MinHoneyToPost = 5;
        _outsider!.Honey = 10;
        _outsider.IsVerified = true;
        _rejectedMembership!.Role = MemberRole.Follower;
        _rejectedMembership.ApprovalStatus = ApprovalStatus.Approved;
        await Context.SaveChangesAsync();

        await _service.VerifyCreatePostAsync(_outsider.Id, _hive.Id);
    }

    [Fact]
    public async Task VerifyCreatePostAsync_UnverifiedUser_ThrowsUnauthorized()
    {
        _hive!.Settings.MinRoleToPost = MemberRole.Guest;
        _hive.Settings.MinHoneyToPost = 0;
        _outsider!.IsVerified = false;
        _rejectedMembership!.Role = MemberRole.Follower;
        _rejectedMembership.ApprovalStatus = ApprovalStatus.Approved;
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyCreatePostAsync(_outsider.Id, _hive.Id));
    }

    [Fact]
    public async Task VerifyCreatePostAsync_MinRoleRestriction_ThrowsUnauthorized()
    {
        _hive!.Settings.MinRoleToPost = MemberRole.Moderator;
        _hive.Settings.MinHoneyToPost = 0;
        _rejectedMembership!.Role = MemberRole.Follower;
        _rejectedMembership.ApprovalStatus = ApprovalStatus.Approved;
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyCreatePostAsync(_outsider!.Id, _hive.Id));
    }

    [Fact]
    public async Task VerifyCreateCommentAsync_HivePostOutsider_DoesNotThrow()
    {
        await _service.VerifyCreateCommentAsync(_outsider!.Id, _hivePost!.Id);
    }

    [Fact]
    public async Task VerifyCreateCommentAsync_PublicPostOutsider_DoesNotThrow()
    {
        await _service.VerifyCreateCommentAsync(_outsider!.Id, _publicPost!.Id);
    }

    [Fact]
    public async Task VerifyDeleteCommentAsync_CommentAuthor_DoesNotThrow()
    {
        await _service.VerifyDeleteCommentAsync(_follower!.Id, _hiveComment!.Id);
    }

    [Fact]
    public async Task VerifyDeleteCommentAsync_Outsider_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyDeleteCommentAsync(_outsider!.Id, _hiveComment!.Id));
    }

    [Fact]
    public async Task VerifyDeleteCommentAsync_Moderator_DoesNotThrow()
    {
        await _service.VerifyDeleteCommentAsync(_moderator!.Id, _hiveComment!.Id);
    }

    [Fact]
    public async Task VerifyEditHiveAsync_Admin_DoesNotThrow()
    {
        await _service.VerifyEditHiveAsync(_admin!.Id, _hive!.Id);
    }

    [Fact]
    public async Task VerifyEditHiveAsync_Moderator_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyEditHiveAsync(_moderator!.Id, _hive!.Id));
    }

    [Fact]
    public async Task VerifyCreateHiveAsync_VerifiedUser_DoesNotThrow()
    {
        _creator!.IsVerified = true;
        await Context.SaveChangesAsync();

        await _service.VerifyCreateHiveAsync(_creator.Id);
    }

    [Fact]
    public async Task VerifyCreateHiveAsync_UnverifiedUser_ThrowsUnauthorized()
    {
        _outsider!.IsVerified = false;
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyCreateHiveAsync(_outsider.Id));
    }

    protected override void SeedDatabase()
    {
        _creator = new User { Username = "creator", Honey = 50, Settings = new() };
        _admin = new User { Username = "admin", Honey = 50, Settings = new() };
        _moderator = new User { Username = "moderator", Honey = 50, Settings = new() };
        _follower = new User { Username = "follower", Honey = 50, Settings = new() };
        _outsider = new User { Username = "outsider", Honey = 0, Settings = new() };

        _hive = new Hive
        {
            Name = "Main Hive",
            Description = "Main hive description",
            Users =
            [
                new() { User = _creator, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Creator },
                new() { User = _admin, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Admin },
                new() { User = _moderator, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Moderator },
                new() { User = _follower, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Follower }
            ],
            Settings = new HiveSettings
            {
                IsPrivate = false,
                JoinRequiresApproval = false,
                PostRequiresApproval = true,
                MinHoneyToPost = 0,
                MinRoleToPost = MemberRole.Follower
            }
        };

        _hivePost = new Post
        {
            Creator = _follower,
            Hive = _hive,
            ApprovalStatus = ApprovalStatus.Pending,
            Polls =
            [
                new Poll
                {
                    Title = "Hive poll",
                    Description = "poll",
                    PollType = PollType.Choice,
                    Candidates = [new Candidate { NormalizedName = "a", Name = "A" }]
                }
            ]
        };

        _publicPost = new Post
        {
            Creator = _creator,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls =
            [
                new Poll
                {
                    Title = "Public poll",
                    Description = "poll",
                    PollType = PollType.Choice,
                    Candidates = [new Candidate { NormalizedName = "a", Name = "A" }]
                }
            ]
        };

        _hiveComment = new Comment
        {
            Post = _hivePost,
            User = _follower,
            Content = "hive comment"
        };

        Context.Users.Add(_outsider);
        Context.Comments.Add(_hiveComment);
        Context.Posts.Add(_publicPost);
        Context.SaveChanges();

        _creatorMembership = Context.HiveUsers.First(h => h.HiveId == _hive.Id && h.UserId == _creator.Id);
        _followerMembership = Context.HiveUsers.First(h => h.HiveId == _hive.Id && h.UserId == _follower.Id);
        _moderatorMembership = Context.HiveUsers.First(h => h.HiveId == _hive.Id && h.UserId == _moderator.Id);

        _rejectedMembership = new HiveUser
        {
            HiveId = _hive.Id,
            UserId = _outsider.Id,
            Role = MemberRole.Follower,
            ApprovalStatus = ApprovalStatus.Rejected
        };
        Context.HiveUsers.Add(_rejectedMembership);
        Context.SaveChanges();
    }
}