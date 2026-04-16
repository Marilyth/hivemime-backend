using Mapster;

public static class MapsterConfiguration
{
    private static TypeAdapterConfig _config = TypeAdapterConfig.GlobalSettings;

    public static void Configure()
    {
        ConfigureComment();
    }

    private static void ConfigureComment()
    {
        _config.NewConfig<Comment, CommentDto>()
            .Map(dest => dest.ReplyCount, src => src.Replies.Count);
    }
}