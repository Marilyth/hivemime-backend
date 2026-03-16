public class PostDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<PollDto> Polls { get; set; }
}

public class PollDto : CreatePollDto
{
    public new int MinValue { get; set; }
    public new int MaxValue { get; set; }

    public new int MinVotes { get; set; }
    public new int MaxVotes { get; set; }
}