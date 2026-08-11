using Microsoft.EntityFrameworkCore;
using Mapster;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json;

public class PostVoteService(HiveMimeContext context,
    AuthorizationService authorizationService,
    HoneyDeltaCalculator honeyDeltaCalculator,
    HybridCache cache)
{
    /// <summary>
    /// Inserts or updates a user's votes on a post.
    /// </summary>
    /// <param name="userId">The ID of the user voting.</param>
    /// <param name="vote">The vote to insert or update.</param>
    public async Task<HoneyDeltaDto<bool>> VoteOnPostAsync(Guid userId, PostVoteDto vote)
    {
        await authorizationService.VerifyVoteOnPostAsync(userId, vote.Id);
        
        Post post = await context.Posts
            .AsNoTracking()
            .Include(p => p.Polls)
            .FirstOrExceptionAsync(p => p.Id == vote.Id);

        IEnumerable<string> validationErrors = ValidatePostVotes(post, vote);

        if (validationErrors.Any())
            throw new ValidationException("Vote validation failed: " + string.Join("; ", validationErrors));
        
        var customCandidateIds = await GetOrCreateCustomCandidates(vote);

        PostVote postVote = await context.PostVotes
            .Include(pv => pv.Votes)
            .FirstOrDefaultAsync(pv => pv.UserId == userId && pv.PostId == vote.Id);
        
        HoneyDeltaDto<bool> delta = honeyDeltaCalculator.FromPostVote(vote);

        // Don't award the user for updating their vote.
        if (postVote is not null)
        {
            context.PostVotes.Remove(postVote);
            delta.HoneyDelta = 0;
        }
            
        postVote = new PostVote
        {
            UserId = userId,
            PostId = post.Id,
            Votes = new List<CandidateVote>()
        };

        context.PostVotes.Add(postVote);
        HashSet<string> candidateValues = new();
        HashSet<Poll> pollsToCheck = [];

        foreach (PollVoteDto pollVote in vote.Polls.DistinctBy(p => p.Id))
        {
            Poll poll = post.Polls.First(p => p.Id == pollVote.Id);
            pollsToCheck.Add(poll);

            foreach (CandidateVoteDto candidateVote in pollVote.Candidates)
            {
                if (candidateVote.Id is null)
                    candidateVote.Id = customCandidateIds[(pollVote.Id, candidateVote.Name.Normalize(false))].Id;

                CandidateVote dbVote;
                switch (poll.PollType)
                {
                    case PollType.Choice:
                        dbVote = new CandidateChoiceVote { CandidateId = candidateVote.Id.Value };
                        break;
                    case PollType.Score:
                        dbVote = new CandidateScoreVote
                        {
                            CandidateId = candidateVote.Id.Value,
                            Score = ((CandidateScoreVoteDto)candidateVote).Score
                        };
                        break;
                    case PollType.Rank:
                        dbVote = new CandidateRankVote
                        {
                            CandidateId = candidateVote.Id.Value,
                            Rank = ((CandidateRankVoteDto)candidateVote).Rank
                        };
                        break;
                    case PollType.Category:
                        dbVote = new CandidateCategoryVote
                        {
                            CandidateId = candidateVote.Id.Value,
                            CategoryId = ((CandidateCategoryVoteDto)candidateVote).CategoryId
                        };
                        break;
                    case PollType.Draw:
                        CandidateDrawVoteDto drawVoteDto = candidateVote as CandidateDrawVoteDto;
                        dbVote = new CandidateDrawVote
                        {
                            CandidateId = candidateVote.Id.Value,
                            CellIndex = drawVoteDto.CellIndex,
                            Value = 1.0 / pollVote.Candidates.Count
                        };
                        break;
                    case PollType.Date:
                        dbVote = new CandidateDateVote
                        {
                            CandidateId = candidateVote.Id.Value,
                            Timestamp = ((CandidateDateVoteDto)candidateVote).Timestamp
                        };
                        break;
                    default:
                        throw new ValidationException("Unknown vote type.");
                }

                postVote.Votes.Add(dbVote);
            }
        }

        // Check if the vote satisfies the poll conditions.
        foreach (Poll poll in pollsToCheck)
        {
            if (poll.ConditionQuery is not null)
            {
                Func<PostVote, bool> predicate = poll.ConditionQuery.ToExpression().Compile();

                if (!predicate(postVote))
                    throw new ValidationException($"Vote does not satisfy poll condition: {poll.ConditionQuery}");
            }
            if (poll.DateFilterQuery is not null)
            {
                Func<PostVote, bool> predicate = poll.DateFilterQuery.ToExpression().Compile();

                if (!predicate(postVote))
                    throw new ValidationException($"Vote does not satisfy date condition: {poll.DateFilterQuery}");
            }
        }

        await context.SaveChangesAsync();
        await honeyDeltaCalculator.AwardScoreAsync(delta, userId);

        return delta;
    }

    /// <summary>
    /// Returns a list of custom candidate suggestions for a poll based on a query string.
    /// Only candidates that start with the query string and are marked as custom will be returned.
    /// </summary>
    /// <param name="pollId">The ID of the poll for which to retrieve custom candidate suggestions.</param>
    /// <param name="query">The query string to filter custom candidates.</param>
    /// <returns>A list of <see cref="CandidateDto"/> representing the custom candidate suggestions.</returns>
    /// <exception cref="ValidationException">Thrown if the query is invalid or the poll does not allow custom answers.</exception>
    public async Task<List<CandidateDto>> GetCustomCandidateSuggestionsAsync(Guid pollId, string query)
    {
        string trimmedQuery = query.Normalize(false);

        if (string.IsNullOrWhiteSpace(trimmedQuery) || trimmedQuery.Length < 3)
            return [];

        return await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, trimmedQuery]), async entry =>
        {
            bool? allowCustomCandidate = await context.Polls
            .AsNoTracking()
            .Where(p => p.Id == pollId)
            .Select(p => p.AllowedCustomCandidateCount > 0)
            .FirstOrDefaultAsync();

            if (allowCustomCandidate == null)
                throw new ValidationException("Poll not found.");

            if (!allowCustomCandidate.Value)
                throw new ValidationException("This poll does not allow custom candidates.");

            return await context.Candidates
                .Where(c => c.PollId == pollId)
                .Where(c => c.NormalizedName.StartsWith(trimmedQuery) && c.IsCustom)
                .OrderBy(c => c.Name)
                .Take(10)
                .ProjectToType<CandidateDto>()
                .ToListAsync();
        });
    }

    private async Task<Dictionary<(Guid, string), Candidate>> GetOrCreateCustomCandidates(PostVoteDto postVoteDto, int retryCount = 0)
    {
        HashSet<(Guid, string, string)> customCandidates = postVoteDto.Polls
            .SelectMany(p => p.Candidates.Where(c => !c.Id.HasValue)
                .Select(candidate => (p.Id, candidate.Name.Normalize(false), candidate.Name)))
            .ToHashSet();

        if (!customCandidates.Any())
            return [];

        IEnumerable<string> customCandidateNames = customCandidates.Select(c => c.Item2).Distinct();
        Dictionary<(Guid, string), Candidate> existingCandidates = 
            context.Candidates.Where(c => c.Poll.PostId == postVoteDto.Id)
                .AsNoTracking()
                .Where(c => c.IsCustom && customCandidateNames.Contains(c.NormalizedName))
                .ToDictionary(c => (c.PollId, c.NormalizedName), c => c);

        foreach (var customCandidate in customCandidates)
        {
            if (!existingCandidates.ContainsKey((customCandidate.Item1, customCandidate.Item2)))
            {
                Candidate newCandidate = new()
                {
                    PollId = customCandidate.Item1,
                    Name = customCandidate.Item3,
                    NormalizedName = customCandidate.Item2,
                    IsCustom = true
                };

                context.Candidates.Add(newCandidate);
                existingCandidates[(customCandidate.Item1, customCandidate.Item2)] = newCandidate;
            }
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key value violates unique constraint") == true)
        {
            if (retryCount >= 3)
                throw new ValidationException("Failed to create custom candidates after multiple attempts due to concurrent modifications. Please try again later.");

            context.ChangeTracker.Clear();

            return await GetOrCreateCustomCandidates(postVoteDto, retryCount + 1);
        }

        return existingCandidates;
    }
    
    private IEnumerable<string> ValidatePostVotes(Post post, PostVoteDto postVote)
    {
        if (post.Polls.Count != postVote.Polls.Count)
        {
            yield return "The number of polls voted on does not match the number of polls in the post.";
            yield break;
        }

        foreach (Poll poll in post.Polls)
        {
            PollVoteDto? pollVote = postVote.Polls.First(pv => pv.Id == poll.Id);

            foreach (string error in ValidateVote(poll, pollVote!))
                yield return error;
        }
    }

    private IEnumerable<string> ValidateVote(Poll poll, PollVoteDto pollVote)
    {
        var candidateGroup = pollVote.Candidates.GroupBy(c => (c.Id, c.Name));

        int votesCount = candidateGroup.Count();
        int minVotesPerCandidate = candidateGroup.Any() ? candidateGroup.Min(c => c.Count()) : 0;
        int maxVotesPerCandidate = candidateGroup.Any() ? candidateGroup.Max(c => c.Count()) : 0;

        // General validation.
        if (votesCount < poll.MinVotes)
            yield return $"Poll requires at least {poll.MinVotes} votes.";

        if (votesCount > poll.MaxVotes)
            yield return $"Poll allows a maximum of {poll.MaxVotes} votes.";

        if (minVotesPerCandidate > 0 && minVotesPerCandidate < poll.MinVotesPerCandidate)
            yield return $"Poll requires at least {poll.MinVotesPerCandidate} votes per candidate.";

        if (maxVotesPerCandidate > 0 && maxVotesPerCandidate > poll.MaxVotesPerCandidate)
            yield return $"Poll allows a maximum of {poll.MaxVotesPerCandidate} votes per candidate.";

        if (pollVote.Candidates.Count(c => c.Id is null) > poll.AllowedCustomCandidateCount)
            yield return $"Poll allows a maximum of {poll.AllowedCustomCandidateCount} custom candidates.";

        // Poll type specific validation.
        switch (poll.PollType)
        {
            case PollType.Rank:
                foreach (string error in ValidateRankPoll(poll, pollVote))
                    yield return error;
                break;
            case PollType.Score:
                foreach (string error in ValidateScorePoll(poll, pollVote))
                    yield return error;
                break;
            case PollType.Date:
                foreach (string error in ValidateDatePoll(poll, pollVote))
                    yield return error;
                break;
            default:
                break;
        }
    }

    private IEnumerable<string> ValidateScorePoll(Poll poll, PollVoteDto pollVote)
    {
        List<double> assignedScores = pollVote.Candidates
            .OfType<CandidateScoreVoteDto>()
            .Select(v => v.Score)
            .ToList();

        if (assignedScores.Any(score => score < poll.MinValue))
            yield return $"Score poll has a minimum value of {poll.MinValue}.";

        if (assignedScores.Any(score => score > poll.MaxValue))
            yield return $"Score poll has a maximum value of {poll.MaxValue}.";
    }

    private IEnumerable<string> ValidateRankPoll(Poll poll, PollVoteDto pollVote)
    {
        List<int> assignedRanks = pollVote.Candidates
            .OfType<CandidateRankVoteDto>()
            .Select(v => v.Rank)
            .ToList();

        int expectedRankCount = assignedRanks.Count;
        HashSet<int> uniqueRanks = [..assignedRanks];

        if (assignedRanks.Any(rank => rank < poll.MinValue))
            yield return $"Ranking poll has a minimum rank of {poll.MinValue}.";

        if (assignedRanks.Any(rank => rank > poll.MaxValue))
            yield return $"Ranking poll has a maximum rank of {poll.MaxValue}.";

        if (uniqueRanks.Count != expectedRankCount)
            yield return "Duplicate values are not allowed in ranking polls.";

        for (int rank = 1; rank <= expectedRankCount; rank++)
        {
            if (!uniqueRanks.Contains(rank))
                yield return $"Ranking poll is missing rank {rank}.";
        }
    }

    private IEnumerable<string> ValidateDatePoll(Poll poll, PollVoteDto pollVote)
    {
        foreach (CandidateDateVoteDto dto in pollVote.Candidates.Cast<CandidateDateVoteDto>())
        {
            if (dto.Timestamp < 0)
                yield return "Date can't be lower than 0.";
        }
    }
}