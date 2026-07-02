using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

public class PostResultService(HiveMimeContext context,
    HybridCache cache)
{
    /// <summary>
    /// Fetches and returns the sum result of a poll, which is the sum of the values of all votes for each candidate.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch results for.</param>
    /// <param name="filter">The filter to apply to the poll results.</param>
    public async Task<PollResultDto<CandidateChoiceResultDto>> GetChoicePollResult(Guid pollId, string filter)
    {
        var sumResults = await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, filter]), async entry =>
        {
            IQueryable<CandidateChoiceVoteWithMetaData> candidateVotes = GetApplicableVotes(pollId, filter)
                .OfType<CandidateChoiceVote>()
                .Select(cv => new CandidateChoiceVoteWithMetaData
                {
                    CandidateId = cv.CandidateId,
                    CandidateName = cv.Candidate.Name,
                    IsCustom = cv.Candidate.IsCustom
                });

            return await candidateVotes
                .GroupBy(v => new { v.CandidateId, v.CandidateName, v.IsCustom })
                .Select(g => new CandidateChoiceResultDto
                {
                    Id = g.Key.CandidateId,
                    Name = g.Key.CandidateName,
                    IsCustom = g.Key.IsCustom,
                    VoteCount = g.Count()
                })
                .ToListAsync();
        });
        

        return ToPollResultDto(sumResults);
    }

    /// <summary>
    /// Fetches and returns a box-plot result of a poll, which includes minimum, first quartile, median, third quartile and maximum of the values of all votes for each candidate.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch results for.</param>
    /// <param name="filter">The filter to apply to the poll results.</param>
    public async Task<PollResultDto<CandidateScoreResultDto>> GetScorePollResult(Guid pollId, string filter)
    {
        var statisticsResults = await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, filter]), async entry =>
        {
            IQueryable<CandidateScoreVoteWithMetaData> candidateVotes = GetApplicableVotes(pollId, filter)
                .OfType<CandidateScoreVote>()
                .Select(cv => new CandidateScoreVoteWithMetaData
                {
                    CandidateId = cv.CandidateId,
                    CandidateName = cv.Candidate.Name,
                    IsCustom = cv.Candidate.IsCustom,
                    Score = cv.Score
                });

            string sql = candidateVotes.AsSingleQuery().ToQueryString();

            Dictionary<string, string> parameters = Regex.Matches(sql, @"-- (@\w+)=(.+)")
                .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.TrimEnd());

            foreach (var param in parameters)
                sql = sql.Replace(param.Key, param.Value);

            return await context.Database.SqlQueryRaw<CandidateScoreResultDto>($"""
                SELECT 
                    "CandidateId" AS "Id",
                    "CandidateName" AS "Name",
                    "IsCustom" AS "IsCustom",
                    MIN("Score") AS "Min",
                    PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY "Score") AS "Q1",
                    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY "Score") AS "Median",
                    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY "Score") AS "Q3",
                    MAX("Score") AS "Max",
                    AVG("Score") AS "Average",
                    Count(*) AS "VoteCount"
                FROM ({sql}) AS "CandidateVotes"
                GROUP BY "CandidateId", "CandidateName", "IsCustom"
                """)
                .ToListAsync();
        });

        return ToPollResultDto(statisticsResults);
    }

    /// <summary>
    /// Fetches and returns a distribution result of a poll, which includes the frequency of each value for each candidate.
    /// </summary>
    /// <param name="pollId">The ID of the poll to fetch results for.</param>
    /// <param name="filter">The filter to apply to the poll results.</param>
    public async Task<PollResultDto<CandidateRankResultDto>> GetRankPollResult(Guid pollId, string filter)
    {
        var distributionResults = await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, filter]), async entry =>
        {
            IQueryable<CandidateRankVoteWithMetaData> candidateVotes = GetApplicableVotes(pollId, filter)
                .OfType<CandidateRankVote>()
                .Select(cv => new CandidateRankVoteWithMetaData
                {
                    CandidateId = cv.CandidateId,
                    CandidateName = cv.Candidate.Name,
                    IsCustom = cv.Candidate.IsCustom,
                    Rank = cv.Rank
                });

            var distributionResults = await candidateVotes
                .GroupBy(v => new { v.CandidateId, v.Rank, v.CandidateName, v.IsCustom })
                .Select(g => new
                {
                    g.Key.CandidateId,
                    g.Key.Rank,
                    g.Key.CandidateName,
                    g.Key.IsCustom,
                    Count = g.Count()
                })
                .ToListAsync();

            var groupedResults = distributionResults
                .GroupBy(r => new { r.CandidateId, r.CandidateName, r.IsCustom })
                .Select(g => new CandidateRankResultDto
                {
                    Id = g.Key.CandidateId,
                    Name = g.Key.CandidateName,
                    IsCustom = g.Key.IsCustom,
                    VoteCount = g.Sum(r => r.Count),
                    Distribution = g.Select(r => new CandidationRankDistributionResultDto
                    {
                        Rank = r.Rank,
                        VoteCount = r.Count
                    }).ToList()
                })
                .ToList();

            return groupedResults;
        });

        return ToPollResultDto(distributionResults);
    }

    public async Task<PollResultDto<CandidateCategoryResultDto>> GetCategoryPollResult(Guid pollId, string filter)
    {
        var distributionResults = await cache.GetOrCreateAsync(CacheHelper.GetCacheKey([pollId, filter]), async entry =>
        {
            IQueryable<CandidateCategoryVoteWithMetaData> candidateVotes = GetApplicableVotes(pollId, filter)
                .OfType<CandidateCategoryVote>()
                .Select(cv => new CandidateCategoryVoteWithMetaData
                {
                    CandidateId = cv.CandidateId,
                    CandidateName = cv.Candidate.Name,
                    IsCustom = cv.Candidate.IsCustom,
                    CategoryId = cv.CategoryId
                });

            var distributionResults = await candidateVotes
                .GroupBy(v => new { v.CandidateId, v.CategoryId, v.CandidateName, v.IsCustom })
                .Select(g => new
                {
                    g.Key.CandidateId,
                    g.Key.CategoryId,
                    g.Key.CandidateName,
                    g.Key.IsCustom,
                    Count = g.Count()
                })
                .ToListAsync();

            var groupedResults = distributionResults
                .GroupBy(r => new { r.CandidateId, r.CandidateName, r.IsCustom })
                .Select(g => new CandidateCategoryResultDto
                {
                    Id = g.Key.CandidateId,
                    Name = g.Key.CandidateName,
                    IsCustom = g.Key.IsCustom,
                    VoteCount = g.Sum(r => r.Count),
                    Distribution = g.Select(r => new CandidationCategoryDistributionResultDto
                    {
                        CategoryId = r.CategoryId,
                        VoteCount = r.Count
                    }).ToList()
                })
                .ToList();

            return groupedResults;
        });

        return ToPollResultDto(distributionResults);
    }
    
    private IQueryable<CandidateVote> GetApplicableVotes(Guid pollId, string filter)
    {
        IQueryable<PostVote> votes = context.PostVotes
            .Where(v => v.Post.Polls.Any(p => p.Id == pollId));

        if (!string.IsNullOrWhiteSpace(filter))
        {
            VoteQueryBase voteQuery = filter.ToVoteQuery();
            Expression<Func<PostVote, bool>> voteExpression = voteQuery.ToExpression();
            votes = votes.Where(v => !v.User.Settings.ProtectVoteOnFilter)
                .Where(voteExpression);
        }

        return votes.SelectMany(v => v.Votes)
            .Where(cv => cv.Candidate.PollId == pollId);
    }

    private PollResultDto<T> ToPollResultDto<T>(List<T> candidateResults, int customCount = 50) where T : CandidateResultDto
    {
        var customResults = candidateResults.Where(c => c.IsCustom)
            .OrderByDescending(c => c.VoteCount)
            .Take(customCount);

        var nonCustomResults = candidateResults.Where(c => !c.IsCustom);

        return new PollResultDto<T>
        {
            Candidates = nonCustomResults.Concat(customResults).ToList()
        };
    }
    
    private abstract class CandidateVoteWithMetaData
    {
        public Guid CandidateId { get; set; }
        public string CandidateName { get; set; }
        public bool IsCustom { get; set; }
    }

    private class CandidateChoiceVoteWithMetaData : CandidateVoteWithMetaData { }

    private class CandidateScoreVoteWithMetaData : CandidateVoteWithMetaData
    {
        public double Score { get; set; }
    }

    private class CandidateRankVoteWithMetaData : CandidateVoteWithMetaData
    {
        public int Rank { get; set; }
    }

    private class CandidateCategoryVoteWithMetaData : CandidateVoteWithMetaData
    {
        public Guid CategoryId { get; set; }
    }
}