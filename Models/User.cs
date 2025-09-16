public class User : EntityWithIdentifier
{
    public string Username { get; set; }
    public string? Email { get; set; }

    public List<Vote> Votes { get; set; }
    public List<Poll> CreatedPolls { get; set; }
}