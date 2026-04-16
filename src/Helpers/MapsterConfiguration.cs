using Mapster;

public static class MapsterConfiguration
{
    private static TypeAdapterConfig _config = TypeAdapterConfig.GlobalSettings;

    public static void Configure()
    {
        ConfigurePost();
        ConfigureComment();
        ConfigureHive();
    }

    private static void ConfigurePost()
    {
        _config.NewConfig<Post, PostDto>()
            .Map(dest => dest.CommentCount, src => src.Comments.Count(c => c.ParentCommentId == null))
            .Map(dest => dest.VoteCount, src => src.PostVotes.Count);
    }

    private static void ConfigureComment()
    {
        _config.NewConfig<Comment, CommentDto>()
            .Map(dest => dest.ReplyCount, src => src.Replies.Count);
    }
    
    private static void ConfigureHive()
    {
        _config.NewConfig<Hive, HiveDto>()
            .Map(dest => dest.PostCount, src => src.Posts.Count)
            .Map(dest => dest.FollowerCount, src => src.Followers.Count);
    }
}