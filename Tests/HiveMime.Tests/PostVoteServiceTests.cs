using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class PostVoteServiceTests : IntegrationTest
{
    private readonly PostVoteService _postVoteService;
    private User? _voter;
    private Poll? _choicePoll;
    private Poll? _scorePoll;
    private Poll? _rankPoll;
    private Poll? _categoryPoll;
    private Poll? _gridPoll;

    public PostVoteServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _postVoteService = Context.GetService<PostVoteService>();
    }

    [Fact]
    public async Task VoteOnPostAsync_ChoicePoll_PersistsChoiceVotes()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 2; p.MaxVotesPerCandidate = 2; });
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id), Choice(candidates[1].Id)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var saved = await Context.PostVotes.Include(pv => pv.Votes).SingleAsync(pv => pv.UserId == _voter.Id);
        Assert.Equal(2, saved.Votes.Count);
        Assert.Contains(saved.Votes, v => v is CandidateChoiceVote cv && cv.CandidateId == candidates[0].Id);
        Assert.Contains(saved.Votes, v => v is CandidateChoiceVote cv && cv.CandidateId == candidates[1].Id);
    }

    [Fact]
    public async Task VoteOnPostAsync_ScorePoll_PersistsScore()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Score; p.MinValue = 1; p.MaxValue = 5; p.MinVotes = 1; p.MaxVotes = 1; p.MaxVotesPerCandidate = 1; });
        var dto = VoteDto(post, (poll.Id, [Score(candidates[0].Id, 3.5)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var vote = await Context.PostVotes.SelectMany(pv => pv.Votes).OfType<CandidateScoreVote>().SingleAsync();
        Assert.Equal(candidates[0].Id, vote.CandidateId);
        Assert.Equal(3.5, vote.Score);
    }

    [Fact]
    public async Task VoteOnPostAsync_RankPoll_PersistsRank()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Rank; p.MinValue = 1; p.MaxValue = 2; p.MinVotes = 1; p.MaxVotes = 2; p.MaxVotesPerCandidate = 2; });
        var dto = VoteDto(post, (poll.Id, [Rank(candidates[0].Id, 1), Rank(candidates[1].Id, 2)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var votes = await Context.PostVotes.SelectMany(pv => pv.Votes).OfType<CandidateRankVote>().ToListAsync();
        Assert.Equal(2, votes.Count);
        Assert.Equal(new HashSet<int> { 1, 2 }, votes.Select(v => v.Rank).ToHashSet());
    }

    [Fact]
    public async Task VoteOnPostAsync_CategoryPoll_PersistsCategory()
    {
        var (post, poll, candidates) = await AddPostAsync(p =>
        {
            p.PollType = PollType.Category;
            p.MinVotes = 1; p.MaxVotes = 1;
            p.MaxVotesPerCandidate = 1;
            p.Categories = [ new Category { Name = "Cat1" }, new Category { Name = "Cat2" } ];
        });
        var categoryId = poll.Categories[0].Id;
        var dto = VoteDto(post, (poll.Id, [Category(candidates[0].Id, categoryId)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var vote = await Context.PostVotes.SelectMany(pv => pv.Votes).OfType<CandidateCategoryVote>().SingleAsync();
        Assert.Equal(categoryId, vote.CategoryId);
    }

    [Fact]
    public async Task VoteOnPostAsync_GridPoll_AssignsEqualWeight()
    {
        var (post, poll, candidates) = await AddPostAsync(p =>
        {
            p.PollType = PollType.Grid;
            p.Rows = 2; p.Columns = 2;
            p.MinVotes = 1; p.MaxVotes = 2;
            p.MaxVotesPerCandidate = 2;
        });
        var dto = VoteDto(post, (poll.Id, [Grid(candidates[0].Id, 0), Grid(candidates[1].Id, 3)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var votes = await Context.PostVotes.SelectMany(pv => pv.Votes).OfType<CandidateGridVote>().ToListAsync();
        Assert.Equal(2, votes.Count);
        Assert.All(votes, v => Assert.Equal(0.5, v.Value));
        Assert.Equal(0, votes.First(v => v.CandidateId == candidates[0].Id).CellIndex);
        Assert.Equal(3, votes.First(v => v.CandidateId == candidates[1].Id).CellIndex);
    }

    [Fact]
    public async Task VoteOnPostAsync_DatePoll_PersistsDateVote()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Date; p.MinVotes = 1; p.MaxVotes = 1; p.MaxVotesPerCandidate = 1; });
        var dto = VoteDto(post, (poll.Id, [Date(candidates[0].Id, 1720000000)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var vote = await Context.PostVotes.SelectMany(pv => pv.Votes).OfType<CandidateDateVote>().SingleAsync();
        Assert.Equal(candidates[0].Id, vote.CandidateId);
        Assert.Equal(1720000000, vote.Timestamp);
    }

    [Fact]
    public async Task VoteOnPostAsync_DatePoll_NegativeTimestamp_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Date; p.MinVotes = 1; p.MaxVotes = 1; });
        var dto = VoteDto(post, (poll.Id, [Date(candidates[0].Id, -1)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("Date can't be lower than 0", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_SecondVote_ReplacesExisting()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 1; p.MaxVotesPerCandidate = 1; });
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);
        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        Assert.Single(await Context.PostVotes.Where(pv => pv.UserId == _voter.Id).ToListAsync());
        var saved = await Context.PostVotes.Include(pv => pv.Votes).SingleAsync(pv => pv.UserId == _voter.Id);
        Assert.Single(saved.Votes);
    }

    [Fact]
    public async Task VoteOnPostAsync_AllPollTypes_PersistsAllVotes()
    {
        var post = await AddMultiPollPostAsync();
        var dto = VoteDto(post,
            (_choicePoll!.Id, [Choice(_choicePoll.Candidates[0].Id), Choice(_choicePoll.Candidates[1].Id)]),
            (_scorePoll!.Id, [Score(_scorePoll.Candidates[0].Id, 4)]),
            (_rankPoll!.Id, [Rank(_rankPoll.Candidates[0].Id, 1), Rank(_rankPoll.Candidates[1].Id, 2)]),
            (_categoryPoll!.Id, [Category(_categoryPoll.Candidates[0].Id, _categoryPoll.Categories[0].Id)]),
            (_gridPoll!.Id, [Grid(_gridPoll.Candidates[0].Id, 1)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var saved = await Context.PostVotes.Include(pv => pv.Votes).SingleAsync(pv => pv.UserId == _voter.Id);
        Assert.Equal(7, saved.Votes.Count);
        Assert.Equal(2, saved.Votes.OfType<CandidateChoiceVote>().Count());
        Assert.Single(saved.Votes.OfType<CandidateScoreVote>());
        Assert.Equal(2, saved.Votes.OfType<CandidateRankVote>().Count());
        Assert.Single(saved.Votes.OfType<CandidateCategoryVote>());
        Assert.Single(saved.Votes.OfType<CandidateGridVote>());
    }

    [Fact]
    public async Task VoteOnPostAsync_BelowMinVotes_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 2; p.MaxVotes = 2; });
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("at least 2 votes", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_AboveMaxVotes_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 1; });
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id), Choice(candidates[1].Id)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("maximum of 1 votes", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_BelowMinVotesPerCandidate_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 2; p.MinVotesPerCandidate = 2; });
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id), Choice(candidates[1].Id)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("at least 2 votes per candidate", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_AboveMaxVotesPerCandidate_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 2; p.MaxVotesPerCandidate = 1; });
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id), Choice(candidates[0].Id)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("maximum of 1 votes per candidate", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_TooManyCustomCandidates_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 2; p.AllowedCustomCandidateCount = 1; });
        var dto = VoteDto(post, (poll.Id, [Choice(null, "Custom1"), Choice(null, "Custom2")]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("maximum of 1 custom candidates", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_RankDuplicate_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Rank; p.MinValue = 1; p.MaxValue = 2; p.MinVotes = 1; p.MaxVotes = 2; });
        var dto = VoteDto(post, (poll.Id, [Rank(candidates[0].Id, 1), Rank(candidates[1].Id, 1)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("Duplicate values are not allowed", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_RankMissingRank_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Rank; p.MinValue = 1; p.MaxValue = 3; p.MinVotes = 1; p.MaxVotes = 3; });
        var dto = VoteDto(post, (poll.Id, [Rank(candidates[0].Id, 1), Rank(candidates[1].Id, 3)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("missing rank 2", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_ScoreBelowMinValue_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Score; p.MinValue = 1; p.MaxValue = 5; p.MinVotes = 1; p.MaxVotes = 1; });
        var dto = VoteDto(post, (poll.Id, [Score(candidates[0].Id, 0)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("minimum value of 1", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_ScoreAboveMaxValue_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.PollType = PollType.Score; p.MinValue = 1; p.MaxValue = 5; p.MinVotes = 1; p.MaxVotes = 1; });
        var dto = VoteDto(post, (poll.Id, [Score(candidates[0].Id, 6)]));

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("maximum value of 5", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_PollCountMismatch_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 1; });
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id)]));
        dto.Polls.Add(new PollVoteDto { Id = Guid.NewGuid(), Candidates = [] });

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
        Assert.Contains("does not match the number of polls", ex.Message);
    }

    [Fact]
    public async Task VoteOnPostAsync_LockedPost_Throws()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 1; });
        post.VotingLockedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();
        var dto = VoteDto(post, (poll.Id, [Choice(candidates[0].Id)]));

        await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.VoteOnPostAsync(_voter!.Id, dto));
    }

    [Fact]
    public async Task VoteOnPostAsync_CustomCandidate_CreatesAndReuses()
    {
        var (post, poll, candidates) = await AddPostAsync(p => { p.MinVotes = 1; p.MaxVotes = 1; p.MaxVotesPerCandidate = 1; p.AllowedCustomCandidateCount = 1; });
        var customName = "My Custom";
        var dto = VoteDto(post, (poll.Id, [Choice(null, customName)]));

        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);
        await _postVoteService.VoteOnPostAsync(_voter!.Id, dto);

        var custom = await Context.Candidates.SingleAsync(c => c.PollId == poll.Id && c.IsCustom);
        Assert.Equal(customName.Normalize(false), custom.NormalizedName);

        Assert.Single(await Context.Candidates.Where(c => c.PollId == poll.Id && c.IsCustom).ToListAsync());
    }

    [Fact]
    public async Task GetCustomCandidateSuggestionsAsync_ShortQuery_ReturnsEmpty()
    {
        var (post, poll, _) = await AddPostAsync(p => { p.AllowedCustomCandidateCount = 1; });

        var result = await _postVoteService.GetCustomCandidateSuggestionsAsync(poll.Id, "ab");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCustomCandidateSuggestionsAsync_UnknownPoll_Throws()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.GetCustomCandidateSuggestionsAsync(Guid.NewGuid(), "abc"));
    }

    [Fact]
    public async Task GetCustomCandidateSuggestionsAsync_NoCustomAllowed_Throws()
    {
        var (post, poll, _) = await AddPostAsync(p => { });

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _postVoteService.GetCustomCandidateSuggestionsAsync(poll.Id, "abc"));
        Assert.Contains("does not allow custom candidates", ex.Message);
    }

    [Fact]
    public async Task GetCustomCandidateSuggestionsAsync_ReturnsMatchingCustomCandidates()
    {
        var (post, poll, _) = await AddPostAsync(p => { p.AllowedCustomCandidateCount = 3; });
        Context.Candidates.AddRange(
            new Candidate { PollId = poll.Id, Name = "Alpha", NormalizedName = "alpha", IsCustom = true },
            new Candidate { PollId = poll.Id, Name = "Beta", NormalizedName = "beta", IsCustom = true },
            new Candidate { PollId = poll.Id, Name = "Gamma", NormalizedName = "gamma", IsCustom = false });
        await Context.SaveChangesAsync();

        var result = await _postVoteService.GetCustomCandidateSuggestionsAsync(poll.Id, "alp");

        var suggestion = Assert.Single(result);
        Assert.Equal("Alpha", suggestion.Name);
        Assert.True(suggestion.IsCustom);
    }

    protected override void SeedDatabase()
    {
        _voter = new User { Username = "voter", IsVerified = true, Settings = new() };
        Context.Users.Add(_voter);
    }

    private static CandidateVoteDto Choice(Guid? id, string name = "A") => new CandidateChoiceVoteDto { Id = id, Name = name };

    private static CandidateVoteDto Score(Guid? id, double value) => new CandidateScoreVoteDto { Id = id, Name = "A", Score = value };

    private static CandidateVoteDto Rank(Guid? id, int rank) => new CandidateRankVoteDto { Id = id, Name = "A", Rank = rank };

    private static CandidateVoteDto Category(Guid? id, Guid categoryId) => new CandidateCategoryVoteDto { Id = id, Name = "A", CategoryId = categoryId };

    private static CandidateVoteDto Grid(Guid? id, int cellIndex) => new CandidateGridVoteDto { Id = id, Name = "A", CellIndex = cellIndex };

    private static CandidateVoteDto Date(Guid? id, long timestamp) => new CandidateDateVoteDto { Id = id, Name = "A", Timestamp = timestamp };

    private PostVoteDto VoteDto(Post post, params (Guid pollId, CandidateVoteDto[] candidates)[] polls)
        => new PostVoteDto
        {
            Id = post.Id,
            Polls = polls.Select(p => new PollVoteDto { Id = p.pollId, Candidates = p.candidates.ToList() }).ToList()
        };

    private async Task<(Post post, Poll poll, List<Candidate> candidates)> AddPostAsync(Action<Poll> configure)
    {
        var poll = new Poll
        {
            Title = "Poll",
            Description = "desc",
            PollType = PollType.Choice,
            Candidates =
            [
                new Candidate { NormalizedName = "a", Name = "A" },
                new Candidate { NormalizedName = "b", Name = "B" }
            ],
            Categories = []
        };
        configure(poll);

        var post = new Post
        {
            Creator = _voter!,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [poll]
        };

        Context.Posts.Add(post);
        await Context.SaveChangesAsync();

        return (post, poll, poll.Candidates);
    }

    private async Task<Post> AddMultiPollPostAsync()
    {
        _choicePoll = new Poll
        {
            Title = "Choice", Description = "d", PollType = PollType.Choice,
            MinVotes = 1, MaxVotes = 2, MaxVotesPerCandidate = 2,
            Candidates = [ new Candidate { NormalizedName = "a", Name = "A" }, new Candidate { NormalizedName = "b", Name = "B" } ],
            Categories = []
        };
        _scorePoll = new Poll
        {
            Title = "Score", Description = "d", PollType = PollType.Score,
            MinValue = 1, MaxValue = 5, MinVotes = 1, MaxVotes = 1, MaxVotesPerCandidate = 1,
            Candidates = [ new Candidate { NormalizedName = "s", Name = "S" } ],
            Categories = []
        };
        _rankPoll = new Poll
        {
            Title = "Rank", Description = "d", PollType = PollType.Rank,
            MinValue = 1, MaxValue = 2, MinVotes = 1, MaxVotes = 2, MaxVotesPerCandidate = 2,
            Candidates = [ new Candidate { NormalizedName = "r1", Name = "R1" }, new Candidate { NormalizedName = "r2", Name = "R2" } ],
            Categories = []
        };
        _categoryPoll = new Poll
        {
            Title = "Category", Description = "d", PollType = PollType.Category,
            MinVotes = 1, MaxVotes = 1, MaxVotesPerCandidate = 1,
            Candidates = [ new Candidate { NormalizedName = "c", Name = "C" } ],
            Categories = [ new Category { Name = "Cat1" }, new Category { Name = "Cat2" } ]
        };
        _gridPoll = new Poll
        {
            Title = "Grid", Description = "d", PollType = PollType.Grid,
            Rows = 2, Columns = 2, MinVotes = 1, MaxVotes = 1, MaxVotesPerCandidate = 1,
            Candidates = [ new Candidate { NormalizedName = "d1", Name = "D1" }, new Candidate { NormalizedName = "d2", Name = "D2" } ],
            Categories = []
        };

        var post = new Post
        {
            Creator = _voter!,
            ApprovalStatus = ApprovalStatus.Approved,
            Polls = [ _choicePoll, _scorePoll, _rankPoll, _categoryPoll, _gridPoll ]
        };

        Context.Posts.Add(post);
        await Context.SaveChangesAsync();
        return post;
    }
}
