using Mapster;

public static class MapsterConfiguration
{
    private static TypeAdapterConfig _config = TypeAdapterConfig.GlobalSettings;

    public static void Configure()
    {
        ConfigureComment();
        ConfigureUser();
        ConfigureCandidate();
        ConfigurePoll();
        ConfigurePost();

        _config.NewConfig<FilterQueryBase, FilterQueryBase>()
            .ConstructUsing(src => src);
    }

    private static void ConfigureComment()
    {
        _config.NewConfig<Comment, CommentDto>()
            .Map(dest => dest.ReplyCount, src => src.Replies.Count)
            .Map(dest => dest.IsOriginalPoster, src => src.UserId == src.Post.CreatorId)
            .Map(dest => dest.Role, src => src.User.JoinedHives.FirstOrDefault(m => m.HiveId == src.Post.HiveId &&
                m.ApprovalStatus == ApprovalStatus.Approved).Role);
    }

    private static void ConfigureUser()
    {
        _config.NewConfig<User, UserProfileDto>()
            .Map(dest => dest.PostCount, src => src.CreatedPosts.Count)
            .Map(dest => dest.CommentCount, src => src.Comments.Count);
    }

    private static void ConfigureCandidate()
    {
        _config.NewConfig<Candidate, CandidateDto>()
            .Map(dest => dest.MediaKeys, src => src.MediaKeys.Select(m => "https://media.mayiscoding.com/" + m));
    }

    private static void ConfigurePoll()
    {
        _config.NewConfig<Poll, PollDto>()
            .Map(dest => dest.MediaKeys, src => src.Candidates.SelectMany(c => c.MediaKeys).Select(m => "https://media.mayiscoding.com/" + m))
            .Map(dest => dest.Candidates, src => src.Candidates.Where(c => !c.IsCustom).OrderBy(c => c.Order))
            .Map(dest => dest.Categories, src => src.Categories.OrderBy(c => c.Order));
    }

    private static void ConfigurePost()
    {
        _config.NewConfig<Post, PostDto>()
            .Map(dest => dest.Role, src => src.Creator.JoinedHives.FirstOrDefault(m => m.HiveId == src.Hive.Id &&
                m.ApprovalStatus == ApprovalStatus.Approved).Role)
            .Map(dest => dest.Polls, src => src.Polls.OrderBy(p => p.Order));
    }
}