using Mapster;

public static class MapsterConfiguration
{
    private static TypeAdapterConfig _config = TypeAdapterConfig.GlobalSettings;

    public static void Configure()
    {
        ConfigureComment();
        ConfigureUser();
    }

    private static void ConfigureComment()
    {
        _config.NewConfig<Comment, CommentDto>()
            .Map(dest => dest.ReplyCount, src => src.Replies.Count);
    }

    private static void ConfigureUser()
    {
        _config.NewConfig<User, UserProfileDto>()
            .Map(dest => dest.PostCount, src => src.CreatedPosts.Count)
            .Map(dest => dest.CommentCount, src => src.Comments.Count);
    }
}