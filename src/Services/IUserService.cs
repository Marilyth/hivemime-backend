public interface IUserService
{
    /// <summary>
    /// Creates a JWT token for the user.
    /// </summary>
    /// <param name="username">The username of the user to create a token for.</param>
    /// <returns>A LoginDto containing the JWT token and username.</returns>
    LoginDto Login(string username);

    /// <summary>
    /// Returns the details of a user, including their settings.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve details for.</param>
    UserDetailsDto GetUserDetails(int userId);

    /// <summary>
    /// Creates a new user with the given username.
    /// </summary>
    /// <param name="username">The username of the user to create.</param>
    User CreateUser(string username);
}
