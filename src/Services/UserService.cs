using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;

public class UserService(HiveMimeContext context, IConfiguration configuration, GeoIPService geoIPService)
{
    /// <summary>
    /// Creates a JWT token for the user.
    /// </summary>
    /// <param name="username">The username of the user to create a token for.</param>
    /// <returns>A LoginDto containing the JWT token and username.</returns>
    public async Task<LoginDto> LoginAsync(string username)
    {
        // TODO: Add security measures / actual login.
        User user = await context.Users.FirstOrDefaultAsync(u => u.Username == username) ?? await CreateUserAsync(username);

        Claim[] claims = [
            new Claim("UserId", user.Id.ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(999),
            signingCredentials: creds
        );

        string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginDto { Token = tokenString, Username = user.Username };
    }

    /// <summary>
    /// Returns the details of a user, including their settings.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve details for.</param>
    public async Task<UserDetailsDto> GetUserDetailsAsync(int userId)
    {
        var user = await context.Users.AsNoTracking().Where(u => u.Id == userId).Include(u => u.Settings).FirstAsync();
        return user.Adapt<UserDetailsDto>();
    }

    /// <summary>
    /// Creates a new user with the given username.
    /// </summary>
    /// <param name="username">The username of the user to create.</param>
    public async Task<User> CreateUserAsync(string username)
    {
        var ipAddress = context.GetService<IHttpContextAccessor>()?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        string? country = await geoIPService.GetCountryOfIPAsync(ipAddress);

        // TODO: Add support for registration using password / emails / OAuth, etc.
        var user = new User()
        {
            Username = username,
            Settings = new()
            {
                Country = country
            }
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }
}