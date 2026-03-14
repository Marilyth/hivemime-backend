public class UserDetailsDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }

    public UserSettingsDto Settings { get; set; }
}

public class UserSettingsDto
{
    public int Id { get; set; }
    public bool ShareDateOfVote { get; set; }
    public bool ShareCountryOfVote { get; set; }
    public bool ShareAgeOfVote { get; set; }
}