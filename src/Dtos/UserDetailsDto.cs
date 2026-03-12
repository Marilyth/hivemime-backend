public class UserDetailsDto
{
    public int Id { get; set; }
    public string Username { get; set; }

    public string? Country { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }

    public UserSettingsDto Settings { get; set; }
}

public class UserSettingsDto
{
    public int Id { get; set; }
    public bool ShowGenderOnVote { get; set; }
    public bool ShowCountryOnVote { get; set; }
    public bool ShowAgeOnVote { get; set; }
}