using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

public static class Algorithms
{
    private static readonly double HotnessBase = Math.Log(100);
    private const int HalflifeDays = 7;
    private const double VoteWeight = 1;
    private const double CommentWeight = 0.5;

    public static UpdateSettersBuilder<Post> UpdateHotness(this UpdateSettersBuilder<Post> builder, int commentDifference = 0, int voteDifference = 0)
    {
        builder.SetProperty(p => p.HotnessLastRecalculatedAt, DateTimeOffset.UtcNow)
               .SetProperty(p => p.Hotness, GetHotnessUpdateExpression(commentDifference, voteDifference));

        if (commentDifference != 0)
            builder.SetProperty(p => p.CommentCount, p => p.CommentCount + commentDifference);
        if (voteDifference != 0)
            builder.SetProperty(p => p.VoteCount, p => p.VoteCount + voteDifference);

        return builder;
    }
    
    public static Expression<Func<Post, double>> GetHotnessUpdateExpression(int commentDifference = 0, int voteDifference = 0) =>
        post => Math.Log(1 + (post.VoteCount + voteDifference) * VoteWeight +
                         (post.CommentCount + commentDifference) * CommentWeight) + // Base score.
                HotnessBase / Math.Pow(2, (DateTimeOffset.UtcNow - post.CreatedAt).TotalDays / HalflifeDays); // Time decayed bonus.

    public static Func<Post, double> HotnessFunction = GetHotnessUpdateExpression().Compile();
}