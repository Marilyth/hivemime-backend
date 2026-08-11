using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public class HoneyDeltaCalculator(HiveMimeContext context, IMemoryCache cache)
{
    public HoneyDeltaDto<PostDto> FromPostDto(PostDto dto)
    {
        int candidatesScore = dto.Polls.Sum(p => p.Candidates.Count);
        int categoriesScore = dto.Polls.Sum(p => p.Categories?.Count ?? 0);
        double descriptionScore = dto.Polls.Sum(p => (p.Description?.Length ?? 0) / 64.0);
        int pollScore = dto.Polls.Count * 5;

        double finalScore = Math.Sqrt(candidatesScore + categoriesScore + descriptionScore + pollScore + 1);

        return new HoneyDeltaDto<PostDto>
        {
            HoneyDelta = finalScore,
            Dto = dto
        };
    }

    public HoneyDeltaDto<CommentDto> FromCommentDto(CommentDto dto)
    {
        double commentLength = dto.Content.Length / 128.0;
        double finalScore = Math.Sqrt(commentLength + 1);
        
        return new HoneyDeltaDto<CommentDto>
        {
            HoneyDelta = finalScore,
            Dto = dto
        };
    }

    public HoneyDeltaDto<bool> FromPostVote(PostVoteDto dto)
    {
        int totalVotes = dto.Polls.Sum(p => p.Candidates.Count());
        double finalScore = Math.Sqrt(totalVotes + 1);

        return new HoneyDeltaDto<bool>
        {
            HoneyDelta = finalScore,
            Dto = true
        };
    }

    public async Task<double> AwardScoreAsync<T>(HoneyDeltaDto<T> delta, Guid userId, [CallerMemberName] string key = null)
    {
        if (delta.HoneyDelta == 0)
            return 0;

        key = $"{key}_{userId}";
        double currentCount = cache.GetOrCreate(key, entry =>
        {
            // Decay expires at midnight.
            entry.AbsoluteExpiration = DateTimeOffset.UtcNow.Date.AddDays(1);
            return 1.0;
        });

        cache.Set(key, currentCount + 1);
        double score = delta.HoneyDelta * Math.Pow(0.9, Math.Min(currentCount, 20));
        delta.HoneyDelta = score;

        await context.Users.QueryableFind(userId)
            .ExecuteUpdateAsync(u => u.SetProperty(user => user.Honey, user => user.Honey + score));

        return score;
    }
}