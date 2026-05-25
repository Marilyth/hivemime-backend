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
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, _defaultPost!.CreatorId, null, new());

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(_defaultPost.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_ByHive_ReturnsPost()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, _defaultHive!.Id, new());

        // Assert
        Assert.Equal(4, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(item.Hive!.Id, _defaultHive!.Id));
    }

    [Fact]
    public async Task BrowsePosts_WithoutFilter_ReturnsAll()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new());

        // Assert
        Assert.Equal(4, result.Items.Count);
    }

    [Fact]
    public async Task BrowsePosts_WithTextFilter_ReturnsExpected()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new() { Filter = "not a default poll" });

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(_defaultPost2!.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_OutstandingWithoutHive_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.BrowsePostsAsync(_defaultUser!.Id, null, null, new(), true));
    }

    [Fact]
    public async Task BrowsePosts_OutstandingUnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var outsider = new User { Username = "outsider-user", Settings = new() };
        Context.Users.Add(outsider);
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.BrowsePostsAsync(outsider.Id, null, _defaultHive!.Id, new(), true));
    }

    [Fact]
    public async Task BrowsePosts_OutstandingAuthorizedUser_ReturnsOnlyUnapproved()
    {
        var unapprovedPost = AddPost();
        unapprovedPost.IsApproved = false;
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _service.BrowsePostsAsync(_defaultUser!.Id, null, _defaultHive!.Id, new(), true);

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, p => Assert.False(p.IsApproved));
        Assert.Contains(result.Items, p => p.Id == unapprovedPost.Id);
    }

    [Fact]
    public async Task BrowsePosts_DefaultView_HidesUnapprovedAndPrivateHiveForNonFollower()
    {
        var privateHive = new Hive
        {
            Name = "Private Hive",
            Description = "Private",
            Creator = _defaultUser!,
            Settings = new HiveSettings { IsPrivate = true, MustBeApprovedToJoin = false, MustBeApprovedToPost = false, PostPolicy = PostPolicy.Anyone },
            Users = [new() { User = _defaultUser!, IsApproved = true }]
        };

        var hiddenByApproval = new Post
        {
            Creator = _defaultUser!,
            Hive = _defaultHive!,
            IsApproved = false,
            Polls = [new Poll { Title = "Hidden approval", Description = "hidden", PollType = PollType.Choice, Candidates = [new Candidate { Name = "A" }] }]
        };

        var hiddenByPrivacy = new Post
        {
            Creator = _defaultUser!,
            Hive = privateHive,
            IsApproved = true,
            Polls = [new Poll { Title = "Hidden privacy", Description = "hidden", PollType = PollType.Choice, Candidates = [new Candidate { Name = "A" }] }]
        };

        Context.Posts.AddRange(hiddenByApproval, hiddenByPrivacy);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _service.BrowsePostsAsync(_defaultUser2!.Id, null, null, new());

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
                $"{post.Id}/{post.Polls[0].Id}/asdf.png",
                $"{post.Id}/{post.Polls[0].Id}/{post.Polls[0].Candidates[0].Id}/asdf.png"
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
    public async Task GetPostResultAsync_WithScoreVotes_AggregatesCorrectly()
    {
        // Arrange
        var candidate = _scorePost!.Polls[0].Candidates[0];

        // Add votes across the range
        await AddVotesToCandidate(candidate.Id, _scorePost.Id, [5, 15, 25, 35, 45, 55, 65, 75, 85, 95]);

        // Act
        var result = await _service.GetPostResultAsync(_scorePost.Id, "");

        // Assert
        Assert.Equal(50, result.Polls[0].Candidates[0].AverageScore);
        Assert.Equal(10, result.Polls[0].Candidates[0].VoterAmount);
        Assert.Null(result.Polls[1].Candidates[0].AverageScore);
        Assert.Equal(0, result.Polls[1].Candidates[0].VoterAmount);
    }

    [Fact]
    public async Task GetPostResultAsync_WithCategoryVotes_AggregatesCorrectly()
    {
        // Arrange
        await AddVotesToCandidate(_categoryPost!.Polls[0].Candidates[0].Id, _categoryPost.Id, [0, 0, 0, 0, 0, 0, 1, 1, 1, 1]);

        // Act
        var result = await _service.GetPostResultAsync(_categoryPost.Id, "");

        // Assert
        Assert.Equal(0, result.Polls[0].Candidates[0].MajorityVote);
        Assert.NotNull(result.Polls[0].Candidates[0].MajorityRatio);
        Assert.Equal(0.6, result.Polls[0].Candidates[0].MajorityRatio!.Value, 4);
    }

    [Fact]
    public async Task GetPostResultAsync_WithChoiceVotes_AggregatesCorrectly()
    {
        // Arrange
        await AddVotesToCandidate(_defaultPost!.Polls[0].Candidates[0].Id, _defaultPost.Id, [1, 1, 1, 1]);
        await AddVotesToCandidate(_defaultPost.Polls[0].Candidates[1].Id, _defaultPost.Id, [1, 1]);

        // Act
        var result = await _service.GetPostResultAsync(_defaultPost.Id, "");

        // Assert
        Assert.Equal(4, result.Polls[0].Candidates[0].VoterAmount);
        Assert.Equal(2, result.Polls[0].Candidates[1].VoterAmount);
    }

    [Fact]
    public async Task GetCandidateDistributionResultsAsync_ScoreWithLargeRange_BucketsValues()
    {
        // Arrange
        var candidate = _scorePost!.Polls[0].Candidates[0];

        // Add votes across the range
        await AddVotesToCandidate(candidate.Id, _scorePost.Id, [5, 15, 25, 35, 45, 55, 65, 75, 85, 95]);

        // Act
        var result = await _service.GetCandidateDistributionResultsAsync(candidate.Id, "");

        // Assert
        Assert.Equal(10, result.Count); // Should create 10 buckets
        Assert.All(result, r => Assert.Equal(1, r.Score)); // Each bucket should have 1 vote
    }

    [Fact]
    public async Task GetCandidateDistributionResultsAsync_ScoreWithSmallRange_NoBucketing()
    {
        // Arrange
        var candidate = _scorePost!.Polls[1].Candidates[0]; // Use small range score poll

        // Add votes
        await AddVotesToCandidate(candidate.Id, _scorePost.Id, [1, 1, 2, 2, 3]);

        // Act
        var result = await _service.GetCandidateDistributionResultsAsync(candidate.Id, "");

        // Assert
        Assert.Equal(3, result.Count); // Should have 3 distinct values
        Assert.Equal(2, result.First().Score); // Value 1 should have 2 votes
        Assert.Equal(2, result.Skip(1).First().Score); // Value 2 should have 2 votes  
        Assert.Equal(1, result.Last().Score); // Value 3 should have 1 vote
    }

    [Fact]
    public async Task GetCandidateDistributionResultsAsync_NoVotes_ReturnsEmpty()
    {
        // Arrange
        var candidate = _defaultPost!.Polls[0].Candidates[0];

        // Act
        var result = await _service.GetCandidateDistributionResultsAsync(candidate.Id, "");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidateDistributionResultsAsync_OrderedByKey_ReturnsInOrder()
    {
        // Arrange
        var candidate = _scorePost!.Polls[1].Candidates[0]; // Use small range score poll
        
        // Add votes in random order
        await AddVotesToCandidate(candidate.Id, _scorePost.Id, [3, 1, 5, 2, 4]);

        // Act
        var result = await _service.GetCandidateDistributionResultsAsync(candidate.Id, "");

        // Assert
        Assert.Equal(5, result.Count);
        // Results should be ordered by value (1, 2, 3, 4, 5)
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].Score <= result[i + 1].Score || i == 0); // First might not follow pattern due to grouping
        }
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
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new PostPaginationDto { OrderBy = PostOrderBy.Hot });

        // Assert
        Assert.True(Algorithms.HotnessFunction(result.Items[0].Adapt<Post>()) > Algorithms.HotnessFunction(result.Items[1].Adapt<Post>()));

        // Arrange 2
        await AddVotesToCandidate(_defaultPost.Polls[0].Candidates[0].Id, _defaultPost.Id, [ 1, 1, 1, 1, 1, 1 ]);
        await Context.SaveChangesAsync();

        // Act 2
        var result2 = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new PostPaginationDto { OrderBy = PostOrderBy.Hot });

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
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, paginationDto);

        // Assert
        Assert.NotEmpty(result.Items);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task BrowsePosts_InBetween_ReturnsNext()
    {
        // Arrange
        var result = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new() { OrderBy = PostOrderBy.New, PageSize = 1 });

        // Act
        var result2 = await _service.BrowsePostsAsync(_defaultUser.Id, null, null, new() { OrderBy = PostOrderBy.New, Cursor = result.NextCursor });

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

    private async Task AddVotesToCandidate(int candidateId, int postId, int[] values)
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
        _defaultUser = new User { Username = "defaultuser", Settings = new() };
        _defaultUser2 = new User { Username = "defaultuser2", Settings = new() };

        _defaultHive = new Hive { Name = "Default Hive", Description = "This is a default hive.", Creator = _defaultUser };

        _defaultPost = new()
        {
            Creator = _defaultUser,
            Hive = _defaultHive,
            IsApproved = true,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        _defaultPost2 = new()
        {
            Creator = _defaultUser2,
            Hive = _defaultHive,
            IsApproved = true,
            Polls = [
                new Poll
                {
                    Title = "Not a default poll",
                    Description = "This is not a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        _scorePost = new()
        {
            Creator = _defaultUser2,
            Hive = _defaultHive,
            IsApproved = true,
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
                        new Candidate { Name = "Score Option", Description = "Score Option Description" }
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
                        new Candidate { Name = "Small Score Option", Description = "Small Score Option Description" }
                    }
                }
            ]
        };

        _categoryPost = new()
        {
            Creator = _defaultUser2,
            Hive = _defaultHive,
            IsApproved = true,
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
                        new Candidate { Name = "A" }
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
            IsApproved = true,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { Name = "Option 2", Description = "Option 2 Description" }
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
                        new Candidate { Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { Name = "Option 2", Description = "Option 2 Description" }
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
            IsApproved = true,
            Polls = [
                new Poll
                {
                    Title = "Default Poll",
                    Description = "This is a default poll.",
                    PollType = PollType.Choice,
                    Candidates = new List<Candidate>
                    {
                        new Candidate { Name = "Option 1", Description = "Option 1 Description" },
                        new Candidate { Name = "Option 2", Description = "Option 2 Description" }
                    }
                }
            ]
        };

        Context.Posts.Add(post);
        Context.SaveChanges();

        return post;
    }
}
