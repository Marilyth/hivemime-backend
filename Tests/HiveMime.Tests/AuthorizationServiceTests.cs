using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class AuthorizationServiceTests : IntegrationTest
{
    private AuthorizationService _service;
    private User? _creator;
    private User? _moderator;
    private User? _follower;
    private User? _outsider;
    private Hive? _hive;
    private Post? _hivePost;
    private Post? _publicPost;
    private Comment? _hiveComment;

    public AuthorizationServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<AuthorizationService>();
    }

    [Fact]
    public async Task VerifyAddModeratorAsync_Creator_DoesNotThrow()
    {
        await _service.VerifyModifyHiveUserAsync(_creator!.Id, _hive!.Id);
    }

    [Fact]
    public async Task VerifyAddModeratorAsync_Outsider_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyModifyHiveUserAsync(_outsider!.Id, _hive!.Id));
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
        _hive!.Settings.PostPolicy = PostPolicy.Anyone;
        _hive.Settings.MinHoneyToPost = 5;
        _outsider!.Honey = 10;
        await Context.SaveChangesAsync();

        await _service.VerifyCreatePostAsync(_outsider.Id, _hive.Id);
    }

    [Fact]
    public async Task VerifyCreatePostAsync_FollowersOnlyRequiresApprovedFollower_ThrowsForOutsider()
    {
        _hive!.Settings.PostPolicy = PostPolicy.FollowersOnly;
        _hive.Settings.MinHoneyToPost = 0;
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyCreatePostAsync(_outsider!.Id, _hive.Id));
    }

    [Fact]
    public async Task VerifyCreateCommentAsync_HivePostOutsider_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyCreateCommentAsync(_outsider!.Id, _hivePost!.Id));
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
    public async Task VerifyEditHiveAsync_Moderator_DoesNotThrow()
    {
        await _service.VerifyEditHiveAsync(_moderator!.Id, _hive!.Id);
    }

    [Fact]
    public async Task VerifyEditHiveAsync_Outsider_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.VerifyEditHiveAsync(_outsider!.Id, _hive!.Id));
    }

    protected override void SeedDatabase()
    {
        _creator = new User { Username = "creator", Honey = 50, Settings = new() };
        _moderator = new User { Username = "moderator", Honey = 50, Settings = new() };
        _follower = new User { Username = "follower", Honey = 50, Settings = new() };
        _outsider = new User { Username = "outsider", Honey = 0, Settings = new() };

        _hive = new Hive
        {
            Name = "Main Hive",
            Description = "Main hive description",
            Creator = _creator,
            Moderators = [_moderator],
            Users = [new() { User = _follower, IsApproved = true }],
            Settings = new HiveSettings
            {
                IsPrivate = false,
                MustBeApprovedToJoin = false,
                MustBeApprovedToPost = true,
                MinHoneyToPost = 0,
                PostPolicy = PostPolicy.FollowersOnly
            }
        };

        _hivePost = new Post
        {
            Creator = _follower,
            Hive = _hive,
            IsApproved = false,
            Polls =
            [
                new Poll
                {
                    Title = "Hive poll",
                    Description = "poll",
                    PollType = PollType.Choice,
                    Candidates = [new Candidate { Name = "A" }]
                }
            ]
        };

        _publicPost = new Post
        {
            Creator = _creator,
            IsApproved = true,
            Polls =
            [
                new Poll
                {
                    Title = "Public poll",
                    Description = "poll",
                    PollType = PollType.Choice,
                    Candidates = [new Candidate { Name = "A" }]
                }
            ]
        };

        _hiveComment = new Comment
        {
            Post = _hivePost,
            User = _follower,
            Content = "hive comment"
        };

        Context.Comments.Add(_hiveComment);
        Context.Posts.Add(_publicPost);
    }
}