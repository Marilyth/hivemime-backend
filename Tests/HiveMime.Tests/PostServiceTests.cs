using Mapster;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Microsoft.Extensions.Caching.Memory;

namespace HiveMime.Tests;

public class PostServiceTests : IntegrationTest
{
    private Hive? _defaultHive;
    private Post? _defaultPost;
    private Post? _defaultPost2;
    private Post? _scorePost;
    private Post? _categoryPost;
    private Post? _privatePost;
    private Post? _unpublishedPost;
    private User? _defaultUser;
    private User? _defaultUser2;

    private PostService _service;

    public PostServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<PostService>();
    }

    [Fact]
    public async Task BrowsePosts_ByUser_ReturnsPost()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, _defaultPost!.CreatorId, null, new(), ApprovalStatus.Approved);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(_defaultPost.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_ByHive_ReturnsPost()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, _defaultHive!.Id, new(), ApprovalStatus.Approved);

        // Assert
        Assert.Equal(4, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(item.Hive!.Id, _defaultHive!.Id));
    }

    [Fact]
    public async Task BrowsePosts_WithoutFilter_ReturnsAll()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new(), ApprovalStatus.Approved);

        // Assert
        Assert.Equal(4, result.Items.Count);
    }

    [Fact]
    public async Task BrowsePosts_WithTextFilter_ReturnsExpected()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new() { Filter = "not default" }, ApprovalStatus.Approved);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(_defaultPost2!.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_OutstandingWithoutHive_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.BrowsePostsAsync(_defaultUser!.Id, null, null, new(), ApprovalStatus.Pending));
    }

    [Fact]
    public async Task BrowsePosts_OutstandingUnauthorizedUser_ThrowsNotFoundException()
    {
        var outsider = new User { Username = "outsider-user", Settings = new() };
        Context.Users.Add(outsider);
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => _service.BrowsePostsAsync(outsider.Id, null, _defaultHive!.Id, new(), ApprovalStatus.Pending));
    }

    [Fact]
    public async Task BrowsePosts_OutstandingAuthorizedUser_ReturnsOnlyUnapproved()
    {
        var unapprovedPost = AddPost();
        unapprovedPost.ApprovalStatus = ApprovalStatus.Pending;
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _service.BrowsePostsAsync(_defaultUser!.Id, null, _defaultHive!.Id, new(), ApprovalStatus.Pending);

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, p => Assert.Equal(ApprovalStatus.Pending, p.ApprovalStatus));
        Assert.Contains(result.Items, p => p.Id == unapprovedPost.Id);
    }

    [Fact]
    public async Task BrowsePosts_DefaultView_HidesUnapprovedAndPrivateHiveForNonFollower()
    {
        var privateHive = new Hive
        {
            Name = "Private Hive",
            Description = "Private",
            Settings = new HiveSettings { IsPrivate = true, JoinRequiresApproval = false, PostRequiresApproval = false, MinRoleToPost = MemberRole.Guest },
            Users = [new() { User = _defaultUser!, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Creator }]
        };

        var hiddenByApproval = new Post
        {
            Creator = _defaultUser!,
            Hive = _defaultHive!,
            ApprovalStatus = ApprovalStatus.Pending,
            Polls = [new Poll { Title = "Hidden approval", Description = "hidden", PollType = PollType.Choice, Candidates = [new Candidate { NormalizedName = "a", Name = "A" }] }]
        };

        var hiddenByPrivacy = new Post
        {
            Creator = _defaultUser!,
            Hive = privateHive,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [new Poll { Title = "Hidden privacy", Description = "hidden", PollType = PollType.Choice, Candidates = [new Candidate { NormalizedName = "a", Name = "A" }] }]
        };

        Context.Posts.AddRange(hiddenByApproval, hiddenByPrivacy);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _service.BrowsePostsAsync(_defaultUser2!.Id, null, null, new(), ApprovalStatus.Approved);

        Assert.DoesNotContain(result.Items, p => p.Id == hiddenByApproval.Id);
        Assert.DoesNotContain(result.Items, p => p.Id == hiddenByPrivacy.Id);
    }

    [Fact]
    public async Task CreatePost_ValidPost_AddsToDatabase()
    {
        // Arrange
        var postDto = new CreatePostDto
        {
            Polls =
            [
                new CreatePollDto
                {
                    Title = "Poll 1",
                    Description = "Description 1",
                    PollType = PollType.Choice,
                    Candidates = [
                        new CreateCandidateDto { Name = "Option 1", Description = "Option 1 Description" },
                        new CreateCandidateDto { Name = "Option 2", Description = "Option 2 Description" }
                    ],
                    Categories = []
                }
            ]
        };

        // Act
        var post = await _service.CreatePostAsync(_defaultUser!.Id, postDto);

        // Assert
        Assert.NotNull(post);
        Assert.NotNull(post.Polls);
        Assert.Single(post.Polls);
        Assert.NotNull(post.Polls[0].Candidates);
        Assert.Equal(2, post.Polls[0].Candidates.Count);
    }

    [Fact]
    public async Task CreatePostAsync_SetsIsDraftTrue()
    {
        // Arrange
        var postDto = new CreatePostDto
        {
            Polls = [ new CreatePollDto { Title = "Poll", Description = "desc", PollType = PollType.Choice, Candidates = [ new CreateCandidateDto { Name = "A" } ], Categories = [] } ]
        };

        // Act
        var post = await _service.CreatePostAsync(_defaultUser!.Id, postDto);
        var dbPost = await Context.Posts.FindAsync(post.Id);

        // Assert
        Assert.NotNull(dbPost);
        Assert.True(dbPost.IsDraft);
    }

    [Fact]
    public async Task PublishPostAsync_SetsIsDraftFalse_AndLinksMedia()
    {
        // Arrange
        var post = AddPost(false);
        
        var mediaServiceMock = new Mock<IMediaService>();
        mediaServiceMock.Setup(m => m.ListObjectsAsync(It.IsAny<string>()))
            .ReturnsAsync([
                $"posts/{post.Id}/{post.Polls[0].Id}/asdf.png",
                $"posts/{post.Id}/{post.Polls[0].Id}/{post.Polls[0].Candidates[0].Id}/asdf.png"
            ]);

        var service = new PostService(Context, Context.GetService<HotnessUpdateQueue>(), Context.GetService<HoneyDeltaCalculator>(), mediaServiceMock.Object, Context.GetService<AuthorizationService>());

        // Act
        await service.PublishPostAsync(_defaultUser!.Id, post.Id);
        var dbPost = await Context.Posts.FindAsync(post.Id);

        // Assert
        Assert.False(dbPost!.IsDraft);
        Assert.Contains(dbPost.Polls[0].Candidates[0].MediaKeys, k => k.EndsWith("asdf.png"));
        Assert.Contains(dbPost.Polls[0].MediaKeys, k => k.EndsWith("asdf.png"));
    }

    [Fact]
    public async Task PublishPostAsync_ThrowsIfNotCreator()
    {
        // Arrange
        var post = AddPost(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.PublishPostAsync(_defaultUser2!.Id, post.Id));
    }


    [Fact]
    public async Task CreatePostAsync_GeneratesPreSignedUrls_WhenMediaRequested()
    {
        // Arrange
        var postDto = new CreatePostDto
        {
            Polls =
            [
                new CreatePollDto
                {
                    Title = "Poll 1",
                    Description = "Description 1",
                    PollType = PollType.Choice,
                    Candidates = [
                        new CreateCandidateDto { Name = "Option 1", Description = "Option 1 Description" },
                        new CreateCandidateDto { Name = "Option 2", Description = "Option 2 Description" }
                    ],
                    Categories = [],
                    Media = new UploadMediaRequestDto { ContentLength = 1, ThumbnailContentLength = 1, ContentType = "image/png" }
                }
            ]
        };

        // Act
        var post = await _service.CreatePostAsync(_defaultUser!.Id, postDto);

        // Assert
        Assert.NotNull(post);
        Assert.All(post.Polls, p => Assert.NotNull(p.MediaUploadUrls));
        Assert.All(post.Polls, p => Assert.All(p.Candidates, c => Assert.Null(c.MediaUploadUrls)));
    }

    [Fact]
    public async Task CreatePost_WithHive_UnverifiedUser_ThrowsUnauthorized()
    {
        // Arrange
        var unverifiedUser = new User { Username = "unverified-post-user", IsVerified = false, Honey = 100, Settings = new() };
        Context.Users.Add(unverifiedUser);
        await Context.SaveChangesAsync();

        Context.HiveUsers.Add(new HiveUser
        {
            HiveId = _defaultHive!.Id,
            UserId = unverifiedUser.Id,
            Role = MemberRole.Follower,
            ApprovalStatus = ApprovalStatus.Approved
        });

        _defaultHive.Settings.MinRoleToPost = MemberRole.Guest;
        _defaultHive.Settings.MinHoneyToPost = 0;
        await Context.SaveChangesAsync();

        var postDto = new CreatePostDto
        {
            HiveId = _defaultHive.Id,
            Polls =
            [
                new CreatePollDto
                {
                    Title = "Poll 1",
                    Description = "Description 1",
                    PollType = PollType.Choice,
                    Candidates = [ new CreateCandidateDto { Name = "Option 1" } ],
                    Categories = []
                }
            ]
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreatePostAsync(unverifiedUser.Id, postDto));
    }

    [Fact]
    public async Task BrowsePosts_OrderByHotness_ReturnsOrdered()
    {
        // Arrange
        await AddVotesToCandidate(_defaultPost2!.Polls[0].Candidates[0].Id, _defaultPost2.Id, [ 1, 1, 1 ]);
        await AddVotesToCandidate(_defaultPost!.Polls[0].Candidates[0].Id, _defaultPost.Id, [ 1 ]);
        
        _defaultPost2.Comments = [new() { Content = "c1", User = _defaultUser }, new() { Content = "c2", User = _defaultUser }];
        _defaultPost.Comments = [new() { Content = "c1", User = _defaultUser2 }];

        await Context.SaveChangesAsync();

        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new PostPaginationDto { OrderBy = PostOrderBy.Hot }, ApprovalStatus.Approved);

        // Assert
        Assert.True(Algorithms.HotnessFunction(result.Items[0].Adapt<Post>()) > Algorithms.HotnessFunction(result.Items[1].Adapt<Post>()));

        // Arrange 2
        await AddVotesToCandidate(_defaultPost.Polls[0].Candidates[0].Id, _defaultPost.Id, [ 1, 1, 1, 1, 1, 1 ]);
        await Context.SaveChangesAsync();

        // Act 2
        var result2 = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new PostPaginationDto { OrderBy = PostOrderBy.Hot }, ApprovalStatus.Approved);

        // Assert 2
        Assert.True(Algorithms.HotnessFunction(result2.Items[0].Adapt<Post>()) > Algorithms.HotnessFunction(result2.Items[1].Adapt<Post>()));
        Assert.NotEqual(result.Items[0].Id, result2.Items[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_AtEnd_StopsPaginating()
    {
        // Arrange
        PostPaginationDto paginationDto = new() { OrderBy = PostOrderBy.New };

        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, paginationDto, ApprovalStatus.Approved);

        // Assert
        Assert.NotEmpty(result.Items);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task BrowsePosts_InBetween_ReturnsNext()
    {
        // Arrange
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new() { OrderBy = PostOrderBy.New, PageSize = 1 }, ApprovalStatus.Approved);

        // Act
        var result2 = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new() { OrderBy = PostOrderBy.New, Cursor = result.NextCursor }, ApprovalStatus.Approved);

        // Assert
        Assert.NotEmpty(result.Items);
        Assert.DoesNotContain(result.Items, r => r.Id != result.Items.Last().Id);
    }

    [Fact]
    public async Task CreatePost_UpdatesVoteCount()
    {
        // Act
        await AddVotesToCandidate(_defaultPost!.Polls[0].Candidates[0].Id, _defaultPost.Id, new[] { 1, 1 });
        var updated = await _service.GetPostAsync(_defaultPost.Id);

        // Assert
        Assert.Equal(2, updated.VoteCount);
    }

    [Fact]
    public async Task AddComment_UpdatesCommentCount()
    {
        // Arrange
        var commentService = Context.GetService<CommentService>();
        var post = _defaultPost!;
        var dto = new CreateCommentDto { PostId = post.Id, Content = "test", ParentCommentId = null };
        // Act
        await commentService.AddCommentAsync(_defaultUser!.Id, dto);
        var updated = await _service.GetPostAsync(post.Id);
        // Assert
        Assert.Equal(1, updated.CommentCount);
    }

    [Fact]
    public async Task GetPollSumResult_ReturnsExpectedSumsPerCandidate()
    {
        // Arrange
        var post = AddPost();
        var poll = post.Polls[0];
        var candidate1 = poll.Candidates[0];
        var candidate2 = poll.Candidates[1];

        await AddVotesToCandidate(candidate1.Id, post.Id, [1, 2, 3]);
        await AddVotesToCandidate(candidate2.Id, post.Id, [4, 5]);

        // Act
        var result = await _service.GetPollSumResult(poll.Id, string.Empty);

        // Assert
        Assert.Equal(2, result.Candidates.Count);

        var candidate1Result = Assert.Single(result.Candidates.Where(c => c.Id == candidate1.Id));
        var candidate2Result = Assert.Single(result.Candidates.Where(c => c.Id == candidate2.Id));

        Assert.Equal(6, candidate1Result.Sum);
        Assert.Equal(3, candidate1Result.VoteCount);

        Assert.Equal(9, candidate2Result.Sum);
        Assert.Equal(2, candidate2Result.VoteCount);
    }

    [Fact]
    public async Task GetPollStatisticsResult_ReturnsExpectedPercentilesAndAverage()
    {
        // Arrange
        var post = AddPost();
        var poll = post.Polls[0];
        var candidate = poll.Candidates[0];

        await AddVotesToCandidate(candidate.Id, post.Id, [1, 2, 3, 4]);

        // Act
        var result = await _service.GetPollStatisticsResult(poll.Id, string.Empty);

        // Assert
        var candidateResult = Assert.Single(result.Candidates);
        Assert.Equal(candidate.Id, candidateResult.Id);
        Assert.Equal(4, candidateResult.VoteCount);
        Assert.Equal(1d, candidateResult.Min, 6);
        Assert.Equal(1.75d, candidateResult.Q1, 6);
        Assert.Equal(2.5d, candidateResult.Median, 6);
        Assert.Equal(3.25d, candidateResult.Q3, 6);
        Assert.Equal(4d, candidateResult.Max, 6);
        Assert.Equal(2.5d, candidateResult.Average, 6);
    }

    [Fact]
    public async Task GetPollDistributionResult_ReturnsExpectedValueFrequencies()
    {
        // Arrange
        var post = AddPost();
        var poll = post.Polls[0];
        var candidate = poll.Candidates[0];

        await AddVotesToCandidate(candidate.Id, post.Id, [1, 1, 2, 3, 3, 3]);

        // Act
        var result = await _service.GetPollDistributionResult(poll.Id, string.Empty);

        // Assert
        var candidateResult = Assert.Single(result.Candidates);
        Assert.Equal(candidate.Id, candidateResult.Id);
        Assert.Equal(6, candidateResult.VoteCount);

        var distribution = candidateResult.Distribution.ToDictionary(d => d.Value, d => d.VoteCount);
        Assert.Equal(3, distribution.Count);
        Assert.Equal(2, distribution[1]);
        Assert.Equal(1, distribution[2]);
        Assert.Equal(3, distribution[3]);
    }

    private async Task AddVotesToCandidate(Guid candidateId, Guid postId, int[] values)
    {
        // Create separate users for each vote to simulate different users voting
        for (int i = 0; i < values.Length; i++)
        {
            var user = new User 
            { 
                Username = $"testuser_{candidateId}_{i}_{DateTime.Now.Ticks}", 
                Settings = new() 
            };
            Context.Users.Add(user);
            await Context.SaveChangesAsync(); // Save to get user ID
            
            var postVote = new PostVote
            {
                UserId = user.Id,
                PostId = postId,
                Votes = new List<CandidateVote>
                {
                    new CandidateVote 
                    { 
                        CandidateId = candidateId, 
                        Value = values[i]
                    }
                }
            };
            
            Context.PostVotes.Add(postVote);
        }
        
        await Context.SaveChangesAsync();
    }

    protected override void SeedDatabase()
    {
        _defaultUser = new User { Username = "defaultuser", IsVerified = true, Settings = new() };
        _defaultUser2 = new User { Username = "defaultuser2", IsVerified = true, Settings = new() };

        _defaultHive = new Hive
        {
            Name = "Default Hive",
            Description = "This is a default hive.",
            Users =
            [
                new() { User = _defaultUser, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Creator },
                new() { User = _defaultUser2, ApprovalStatus = ApprovalStatus.Approved, Role = MemberRole.Follower }
            ]
        };

        _defaultPost = new()
        {
            Creator = _defaultUser,
            Hive = _defaultHive,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "option 1", Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { NormalizedName = "option 2", Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        _defaultPost2 = new()
        {
            Creator = _defaultUser2,
            Hive = _defaultHive,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [
                new Poll
                {
                    Title = "A not default poll",
                    Description = "This is not a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "option 1", Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { NormalizedName = "option 2", Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        _scorePost = new()
        {
            Creator = _defaultUser2,
            Hive = _defaultHive,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [
                new Poll
                {
                    Title = "Score Poll",
                    Description = "This is a score poll.",
                    PollType = PollType.Score,
                    MinValue = 1,
                    MaxValue = 100, // Large range to trigger bucketing
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "score option", Name = "Score Option", Description = "Score Option Description" }
                    }
                },
                new Poll
                {
                    Title = "Small Range Score Poll", 
                    Description = "This is a small range score poll.",
                    PollType = PollType.Score,
                    MinValue = 1,
                    MaxValue = 5, // Small range, no bucketing
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "small score option", Name = "Small Score Option", Description = "Small Score Option Description" }
                    }
                }
            ]
        };

        _categoryPost = new()
        {
            Creator = _defaultUser2,
            Hive = _defaultHive,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [
                new Poll
                {
                    Title = "Category Poll",
                    Description = "This is a category poll.",
                    PollType = PollType.Category,
                    MinValue = 1,
                    MaxValue = 2,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "a", Name = "A" }
                    },
                    Categories = new List<Category>
                    {
                        new Category { Name = "Category 1" },
                        new Category { Name = "Category 2" }
                    }
                }
            ]
        };

        _privatePost = new()
        {
            Creator = _defaultUser,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "option 1", Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { NormalizedName = "option 2", Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        _unpublishedPost = new()
        {
            Creator = _defaultUser,
            Hive = _defaultHive,
            IsDraft = true,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "option 1", Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { NormalizedName = "option 2", Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        Context.Posts.Add(_defaultPost);
        Context.Posts.Add(_defaultPost2);
        Context.Posts.Add(_scorePost);
        Context.Posts.Add(_categoryPost);
    }

    private Post AddPost(bool isPublished = true)
    {
        var post = new Post
        {
            Creator = _defaultUser!,
            Hive = _defaultHive!,
            IsDraft = !isPublished,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { NormalizedName = "option 1", Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { NormalizedName = "option 2", Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        Context.Posts.Add(post);
        Context.SaveChanges();

        return post;
    }
}
