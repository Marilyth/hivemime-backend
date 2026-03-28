public class UserDetailsDto
{
    public string Username { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }

    public UserSettingsDto Settings { get; set; }
}

public class UserSettingsDto
{
    public string? Country { get; set; }
    public bool ShareDateOnVote { get; set; }
    public bool ShareCountryOnVote { get; set; }
    public bool ShareAgeOnVote { get; set; }
}