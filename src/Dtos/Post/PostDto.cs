public class PostDto
{
    public HiveDto? Hive { get; set; }
    public UserDto Creator { get; set; }
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<PollDto> Polls { get; set; }
    public int CommentCount { get; set; }
    public int VoteCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PollDto : CreatePollDto
{
    public new int MinValue { get; set; }
    public new int MaxValue { get; set; }

    public new int MinVotes { get; set; }
    public new int MaxVotes { get; set; }
}