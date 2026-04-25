using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

public static class Algorithms
{
    /// <summary>
    /// A value to boost new posts over old ones.
    /// </summary>
    private static readonly double BaseScore = 100;

    /// <summary>
    /// The amount of hours it takes for the hotness to halve.
    /// </summary>
    private const double HalflifeHours = 24;

    /// <summary>
    /// The weight of a vote in the hotness calculation.
    /// </summary>
    private const double VoteWeight = 1;

    /// <summary>
    /// The weight of a comment in the hotness calculation.
    /// </summary>
    private const double CommentWeight = 0.5;

    /// <summary>
    /// The amount of honey by which each new level delta is increased.
    /// </summary>
    private const double HoneyScale = 20;

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
        post => Math.Log(BaseScore +
                         (post.VoteCount + voteDifference) * VoteWeight +
                         (post.CommentCount + commentDifference) * CommentWeight) /
                Math.Pow(2, (DateTimeOffset.UtcNow - post.CreatedAt).TotalHours / HalflifeHours);

    public static Func<Post, double> HotnessFunction = GetHotnessUpdateExpression().Compile();

    public static double GetHoneyLevel(double honey)
        => Math.Floor(Math.Sqrt(honey / HoneyScale));

    public static double GetLevelHoney(double level)
        => level * level * HoneyScale;
}