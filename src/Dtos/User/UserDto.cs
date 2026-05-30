public class UserDto : IHasIdentifier
{
    public int Id { get; set; }
    public string Username { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public double Honey { get; set; }
    public bool IsVerified { get; set; }
}