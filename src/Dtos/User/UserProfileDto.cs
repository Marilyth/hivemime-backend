public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public double Honey { get; set; }
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastLogin { get; set; }
}