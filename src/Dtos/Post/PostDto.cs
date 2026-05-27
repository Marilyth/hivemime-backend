public class PostDto : IHasIdentifier
{
    public HiveDto? Hive { get; set; }
    public UserDto Creator { get; set; }
    public int Id { get; set; }
    public List<PollDto> Polls { get; set; }
    public int CommentCount { get; set; }
    public int VoteCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public double Hotness { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public bool IsDraft { get; set; }
}
