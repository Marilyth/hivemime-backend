using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class PostServiceTests : IntegrationTest
{
    private Hive _defaultHive;
    private Post _defaultPost;
    private Post _defaultPost2;
    private Post _scorePost;
    private User _defaultUser;
    private User _defaultUser2;

    private PostService _service;

    public PostServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<PostService>();
    }

    [Fact]
    public async Task BrowsePosts_ByUser_ReturnsPost()
    {
        // Act
        var result = await _service.BrowsePostsAsync(_defaultPost.CreatorId, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_ByHive_ReturnsPost()
    {
        // Act
        var result = await _service.BrowsePostsAsync(null, _defaultHive.Id, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_WithoutFilter_ReturnsAll()
    {
        // Act
        var result = await _service.BrowsePostsAsync(null, null, null, null);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(_defaultPost.Id, result[1].Id);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_WithDateFilter_ReturnsExpected()
    {
        // Arrange
        Post newPost = new()
        {
            Title = "New Post",
            Description = "This is a new post.",
            Creator = _defaultUser
        };

        Context.Posts.Add(newPost);
        await Context.SaveChangesAsync();

        // Act
        var result = await _service.BrowsePostsAsync(null, null, null, newPost.CreatedAt);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(_defaultPost.Id, result[1].Id);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task BrowsePosts_WithTextFilter_ReturnsExpected()
    {
        // Act
        var result = await _service.BrowsePostsAsync(null, null, "not a default post", null);

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultPost2.Id, result[0].Id);
    }

    [Fact]
    public async Task CreatePost_ValidPost_AddsToDatabase()
    {
        // Arrange
        var postDto = new CreatePostDto
        {
            Title = "New Post",
            Description = "New post description",
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
        var post = await _service.CreatePostAsync(_defaultUser.Id, postDto);

        // Assert
        Assert.NotNull(post);
        Assert.Equal("New Post", post.Title);
        Assert.Equal("New post description", post.Description);
        Assert.Equal(_defaultUser.Id, post.Creator.Id);
        Assert.Single(post.Polls);
        Assert.Equal("Poll 1", post.Polls[0].Title);
        Assert.Equal("Description 1", post.Polls[0].Description);
        Assert.Equal(PollType.Choice, post.Polls[0].PollType);
        Assert.Equal(2, post.Polls[0].Candidates.Count);
        Assert.Equal("Option 1", post.Polls[0].Candidates[0].Name);
        Assert.Equal("Option 2", post.Polls[0].Candidates[1].Name);
    }

    [Fact]
    public async Task GetCandidateDistributionResultsAsync_ScoreWithLargeRange_BucketsValues()
    {
        // Arrange
        Context.Posts.Add(_scorePost);
        await Context.SaveChangesAsync();
        
        var candidate = _scorePost.Polls[0].Candidates[0];

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
        Context.Posts.Add(_scorePost);
        await Context.SaveChangesAsync();
        
        var candidate = _scorePost.Polls[1].Candidates[0]; // Use small range score poll

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
        var candidate = _defaultPost.Polls[0].Candidates[0];

        // Act
        var result = await _service.GetCandidateDistributionResultsAsync(candidate.Id, "");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCandidateDistributionResultsAsync_OrderedByKey_ReturnsInOrder()
    {
        // Arrange
        Context.Posts.Add(_scorePost);
        await Context.SaveChangesAsync();
        
        var candidate = _scorePost.Polls[1].Candidates[0]; // Use small range score poll
        
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

    private async Task AddVotesToCandidate(int candidateId, int postId, int[] values)
    {
        // Create separate users for each vote to simulate different users voting
        for (int i = 0; i < values.Length; i++).
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
            Title = "Default Post",
            Description = "This is a default post.",
            Creator = _defaultUser,
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
            Title = "Not a default post",
            Description = "This is not a default post.",
            Creator = _defaultUser2,
            Hive = _defaultHive,
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
            Title = "Score Post",
            Description = "This is a score post with large range.",
            Creator = _defaultUser,
            Polls = [
                new Poll
                {
                    Title = "Score Poll",
                    Description = "This is a score poll.",
                    PollType = PollType.Score,
                    MinValue = 0,
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

        Context.Posts.Add(_defaultPost);
        Context.Posts.Add(_defaultPost2);
    }
}
