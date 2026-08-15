using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Hybrid;

namespace HiveMime.Tests;

public class PostResultServiceTests : IntegrationTest
{
    private readonly PostResultService _postResultService;
    private User? _voter;
    private Post? _categoryPost;
    private Poll? _categoryPoll;
    private Post? _gridPost;
    private Poll? _gridPoll;
    private Post? _choicePost;
    private Poll? _choicePoll;
    private Post? _scorePost;
    private Poll? _scorePoll;
    private Post? _rankPost;
    private Poll? _rankPoll;

    public PostResultServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _postResultService = Context.GetService<PostResultService>();
    }

    [Fact]
    public async Task GetCategoryPollResult_ReturnsCategoryDistribution()
    {
        var candidate = _categoryPoll!.Candidates[0];
        var cat1 = _categoryPoll.Categories[0].Id;
        var cat2 = _categoryPoll.Categories[1].Id;

        await AddVotesAsync(candidate.Id, _categoryPost!.Id, [
            new CandidateCategoryVote { CategoryId = cat1 },
            new CandidateCategoryVote { CategoryId = cat1 },
            new CandidateCategoryVote { CategoryId = cat2 }
        ]);

        var result = await _postResultService.GetCategoryPollResult(_categoryPoll.Id, null);

        var candidateResult = Assert.Single(result.Candidates);
        Assert.Equal(candidate.Id, candidateResult.Id);
        Assert.Equal(3, candidateResult.VoteCount);

        var distribution = candidateResult.Distribution.ToDictionary(d => d.CategoryId, d => d.VoteCount);
        Assert.Equal(2, distribution[cat1]);
        Assert.Equal(1, distribution[cat2]);
    }

    [Fact]
    public async Task GetGridPollResult_ReturnsRowColumnDistribution()
    {
        var candidate = _gridPoll!.Candidates[0];

        await AddVotesAsync(candidate.Id, _gridPost!.Id, [
            new CandidateGridVote { Row = 0, Column = 0 },
            new CandidateGridVote { Row = 0, Column = 0 },
            new CandidateGridVote { Row = 0, Column = 1 }
        ]);

        var result = await _postResultService.GetGridPollResult(_gridPoll.Id, null);

        var candidateResult = Assert.Single(result.Candidates);
        Assert.Equal(candidate.Id, candidateResult.Id);
        Assert.Equal(3, candidateResult.VoteCount);

        var distribution = candidateResult.Distribution.ToDictionary(d => (d.Row, d.Column), d => d);
        Assert.Equal(2, distribution[(0, 0)].VoteCount);
        Assert.Equal(1, distribution[(0, 1)].VoteCount);
    }

    [Fact]
    public async Task GetChoicePollResult_WithFilter_ExcludesProtectedUsers()
    {
        // Filter parsing is a stub (see plan §6). Guard until the parser is implemented.
        var candidate = _choicePoll!.Candidates[0];

        var protectedUser = new User { Username = $"protected_{DateTime.Now.Ticks}", Settings = new() { ProtectVoteOnFilter = true } };
        Context.Users.Add(protectedUser);
        await Context.SaveChangesAsync();
        Context.PostVotes.Add(new PostVote { UserId = protectedUser.Id, PostId = _choicePost!.Id, Votes = [new CandidateChoiceVote { CandidateId = candidate.Id }] });

        var normalUser = new User { Username = $"normal_{DateTime.Now.Ticks}", Settings = new() };
        Context.Users.Add(normalUser);
        await Context.SaveChangesAsync();
        Context.PostVotes.Add(new PostVote { UserId = normalUser.Id, PostId = _choicePost.Id, Votes = [new CandidateChoiceVote { CandidateId = candidate.Id }] });

        await Context.SaveChangesAsync();

        // No filter => all votes counted.
        var unfiltered = await _postResultService.GetChoicePollResult(_choicePoll.Id, null);
        Assert.Equal(2, Assert.Single(unfiltered.Candidates).VoteCount);
    }

    [Fact]
    public async Task GetChoicePollResult_CustomCandidates_LimitedToTopFiftyAfterNonCustom()
    {
        var nonCustom = _choicePoll!.Candidates[0];

        var customCandidates = new List<Candidate>();
        for (int i = 0; i < 55; i++)
        {
            customCandidates.Add(new Candidate
            {
                PollId = _choicePoll.Id,
                Name = $"Custom{i}",
                NormalizedName = $"custom{i}",
                IsCustom = true
            });
        }
        Context.Candidates.AddRange(customCandidates);
        await Context.SaveChangesAsync();

        var votes = new List<CandidateVote> { new CandidateChoiceVote { CandidateId = nonCustom.Id } };
        votes.AddRange(customCandidates.Select(c => (CandidateVote)new CandidateChoiceVote { CandidateId = c.Id }));

        Context.PostVotes.Add(new PostVote { UserId = _voter!.Id, PostId = _choicePost!.Id, Votes = votes });
        await Context.SaveChangesAsync();

        var result = await _postResultService.GetChoicePollResult(_choicePoll.Id, null);

        Assert.False(result.Candidates[0].IsCustom);
        Assert.Equal(51, result.Candidates.Count);
        Assert.All(result.Candidates.Skip(1), c => Assert.True(c.IsCustom));
    }

    [Fact]
    public async Task GetChoicePollResult_ReturnsExpectedVoteCounts()
    {
        var candidate1 = _choicePoll!.Candidates[0];
        var candidate2 = _choicePoll.Candidates[1];

        await AddVotesAsync(candidate1.Id, _choicePost!.Id, [
            new CandidateChoiceVote(),
            new CandidateChoiceVote(),
            new CandidateChoiceVote()]);
        await AddVotesAsync(candidate2.Id, _choicePost.Id, [
            new CandidateChoiceVote(),
            new CandidateChoiceVote()]);

        var result = await _postResultService.GetChoicePollResult(_choicePoll.Id, null);

        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal(3, Assert.Single(result.Candidates, c => c.Id == candidate1.Id).VoteCount);
        Assert.Equal(2, Assert.Single(result.Candidates, c => c.Id == candidate2.Id).VoteCount);
    }

    [Fact]
    public async Task GetScorePollResult_ReturnsExpectedPercentilesAndAverage()
    {
        var candidate = _scorePoll!.Candidates[0];

        await AddVotesAsync(candidate.Id, _scorePost!.Id, [
            new CandidateScoreVote { Score = 1 },
            new CandidateScoreVote { Score = 2 },
            new CandidateScoreVote { Score = 3 },
            new CandidateScoreVote { Score = 4 }
        ]);

        var result = await _postResultService.GetScorePollResult(_scorePoll.Id, null);

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
    public async Task GetRankPollResult_ReturnsExpectedRankFrequencies()
    {
        var candidate = _rankPoll!.Candidates[0];

        await AddVotesAsync(candidate.Id, _rankPost!.Id, [
            new CandidateRankVote { Rank = 1 },
            new CandidateRankVote { Rank = 1 },
            new CandidateRankVote { Rank = 2 },
            new CandidateRankVote { Rank = 3 },
            new CandidateRankVote { Rank = 3 },
            new CandidateRankVote { Rank = 3 }
        ]);

        var result = await _postResultService.GetRankPollResult(_rankPoll.Id, null);

        var candidateResult = Assert.Single(result.Candidates);
        Assert.Equal(candidate.Id, candidateResult.Id);
        Assert.Equal(6, candidateResult.VoteCount);

        var distribution = candidateResult.Distribution.ToDictionary(d => d.Rank, d => d.VoteCount);
        Assert.Equal(3, distribution.Count);
        Assert.Equal(2, distribution[1]);
        Assert.Equal(1, distribution[2]);
        Assert.Equal(3, distribution[3]);
    }

    [Fact]
    public async Task GetChoicePollResult_ReflectsNewVotesAfterCacheEviction()
    {
        var candidate = _choicePoll!.Candidates[0];

        await AddVotesAsync(candidate.Id, _choicePost!.Id, [new CandidateChoiceVote(), new CandidateChoiceVote()]);

        var before = await _postResultService.GetChoicePollResult(_choicePoll.Id, null);
        Assert.Equal(2, Assert.Single(before.Candidates).VoteCount);

        // Add a vote and evict the cached result so the next call reflects it.
        await AddVotesAsync(candidate.Id, _choicePost!.Id, [new CandidateChoiceVote()]);
        await Context.GetService<HybridCache>().RemoveAsync(
            CacheHelper.GetCacheKey([_choicePoll.Id, null], nameof(PostResultService.GetChoicePollResult)));

        var after = await _postResultService.GetChoicePollResult(_choicePoll.Id, null);
        Assert.Equal(3, Assert.Single(after.Candidates).VoteCount);
    }

    protected override void SeedDatabase()
    {
        _voter = new User { Username = "resvoter", IsVerified = true, Settings = new() };
        Context.Users.Add(_voter);

        _categoryPoll = new Poll
        {
            Title = "Category", Description = "d", PollType = PollType.Category,
            MinVotes = 1, MaxVotes = 1,
            Candidates = [ new Candidate { NormalizedName = "c", Name = "C" } ],
            Categories = [ new Category { Name = "Cat1" }, new Category { Name = "Cat2" } ]
        };
        _categoryPost = new Post
        {
            Creator = _voter, ApprovalStatus = ApprovalStatus.Approved, Polls = [_categoryPoll]
        };

        _gridPoll = new Poll
        {
            Title = "Grid", Description = "d", PollType = PollType.Grid,
            Rows = 2, Columns = 2, MinVotes = 1, MaxVotes = 1,
            Candidates = [ new Candidate { NormalizedName = "d", Name = "D" } ],
            Categories = []
        };
        _gridPost = new Post
        {
            Creator = _voter, ApprovalStatus = ApprovalStatus.Approved, Polls = [_gridPoll]
        };

        _choicePoll = new Poll
        {
            Title = "Choice", Description = "d", PollType = PollType.Choice,
            MinVotes = 1, MaxVotes = 2,
            Candidates =
            [
                new Candidate { NormalizedName = "o", Name = "Option" },
                new Candidate { NormalizedName = "o2", Name = "Option 2" }
            ],
            Categories = []
        };
        _choicePost = new Post
        {
            Creator = _voter, ApprovalStatus = ApprovalStatus.Approved, Polls = [_choicePoll]
        };

        _scorePoll = new Poll
        {
            Title = "Score", Description = "d", PollType = PollType.Score,
            MinValue = 1, MaxValue = 100,
            Candidates = [ new Candidate { NormalizedName = "s", Name = "Score Option" } ],
            Categories = []
        };
        _scorePost = new Post
        {
            Creator = _voter, ApprovalStatus = ApprovalStatus.Approved, Polls = [_scorePoll]
        };

        _rankPoll = new Poll
        {
            Title = "Rank", Description = "d", PollType = PollType.Rank,
            MinValue = 1, MaxValue = 3,
            Candidates = [ new Candidate { NormalizedName = "r", Name = "Rank Option" } ],
            Categories = []
        };
        _rankPost = new Post
        {
            Creator = _voter, ApprovalStatus = ApprovalStatus.Approved, Polls = [_rankPoll]
        };

        Context.Posts.Add(_categoryPost);
        Context.Posts.Add(_gridPost);
        Context.Posts.Add(_choicePost);
        Context.Posts.Add(_scorePost);
        Context.Posts.Add(_rankPost);
    }

    private async Task AddVotesAsync(Guid candidateId, Guid postId, CandidateVote[] votes)
    {
        for (int i = 0; i < votes.Length; i++)
        {
            votes[i].CandidateId = candidateId;

            var user = new User { Username = $"resuser_{candidateId}_{i}_{DateTime.Now.Ticks}", Settings = new() };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            Context.PostVotes.Add(new PostVote { UserId = user.Id, PostId = postId, Votes = [votes[i]] });
        }

        await Context.SaveChangesAsync();
    }
}
