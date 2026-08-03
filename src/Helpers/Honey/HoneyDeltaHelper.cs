using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public class HoneyDeltaCalculator(HiveMimeContext context, IMemoryCache cache)
{
    public async Task<HoneyDeltaDto<PostDto>> FromPostDtoAsync(Guid userId, PostDto dto)
    {
        int candidatesScore = dto.Polls.Sum(p => p.Candidates.Count);
        int categoriesScore = dto.Polls.Sum(p => p.Categories?.Count ?? 0);
        double descriptionScore = dto.Polls.Sum(p => (p.Description?.Length ?? 0) / 64.0);
        int pollScore = dto.Polls.Count * 5;

        double finalScore = Math.Sqrt(candidatesScore + categoriesScore + descriptionScore + pollScore + 1);
        finalScore = await AwardScoreAsync(userId, finalScore);

        return new HoneyDeltaDto<PostDto>
        {
            HoneyDelta = finalScore,
            Dto = dto
        };
    }

    public async Task<HoneyDeltaDto<CommentDto>> FromCommentDtoAsync(Guid userId, CommentDto dto)
    {
        double commentLength = dto.Content.Length / 128.0;

        double finalScore = Math.Sqrt(commentLength + 1);
        finalScore = await AwardScoreAsync(userId, finalScore);
        
        return new HoneyDeltaDto<CommentDto>
        {
            HoneyDelta = finalScore,
            Dto = dto
        };
    }

    public async Task<HoneyDeltaDto<bool>> FromPostVoteAsync(Guid userId, PostVoteDto dto)
    {
        int totalVotes = dto.Polls.Sum(p => p.Candidates.Count());

        double finalScore = Math.Sqrt(totalVotes + 1);
        finalScore = await AwardScoreAsync(userId, finalScore);

        return new HoneyDeltaDto<bool>
        {
            HoneyDelta = finalScore,
            Dto = true
        };
    }

    private async Task<double> AwardScoreAsync(Guid userId, double score, [CallerMemberName] string key = null)
    {
        key = $"{key}_{userId}";
        double currentCount = cache.GetOrCreate(key, entry =>
        {
            // Decay expires at midnight.
            entry.AbsoluteExpiration = DateTimeOffset.UtcNow.Date.AddDays(1);
            return 1.0;
        });

        cache.Set(key, currentCount + 1);
        score *= Math.Pow(0.9, Math.Min(currentCount, 20));

        await context.Users.QueryableFind(userId)
            .ExecuteUpdateAsync(u => u.SetProperty(user => user.Honey, user => user.Honey + score));

        return score;
    }
}