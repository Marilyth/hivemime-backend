public static class UserExtensions
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username
        };
    }

    public static UserDetailsDto ToDetailsDto(this User user)
    {
        return new UserDetailsDto
        {
            Id = user.Id,
            Username = user.Username,

            Gender = user.Gender,
            Country = user.Country,
            DateOfBirth = user.DateOfBirth,

            Settings = user.Settings.ToSettingsDto()
        };
    }

    public static UserSettingsDto ToSettingsDto(this UserSettings settings)
    {
        return new UserSettingsDto
        {
            ShowAgeOnVote = settings.ShowAgeOnVote,
            ShowCountryOnVote = settings.ShowCountryOnVote,
            ShowGenderOnVote = settings.ShowGenderOnVote
        };
    }
}