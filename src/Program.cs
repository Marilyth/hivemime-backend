using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

public class Program
{
    private static WebApplication _app;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        // Add services to the container.
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers();

        // For testing purposes, use this command.
        // sudo docker run -d --name my-postgres -e POSTGRES_USER=myuser -e POSTGRES_PASSWORD=mypassword -e POSTGRES_DB=mydb -p 5432:5432 postgres:17
        services.AddDbContext<HiveMimeContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("TestConnection")));

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });
        services.AddAuthorization();

        services.AddSwaggerGen(setup =>
        {
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Name = "JWT Authentication",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

            setup.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtSecurityScheme, Array.Empty<string>() }
            });
        });

        services.AddHttpContextAccessor();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        // Add custom services.
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped(s => s.GetService<IHttpContextAccessor>().HttpContext.User);

        _app = builder.Build();

        _app.UseCors("AllowAll");

        // Configure the HTTP request pipeline.
        if (_app.Environment.IsDevelopment())
        {
            _app.UseSwagger();
            _app.UseSwaggerUI();
        }

        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseHttpsRedirection();
        _app.MapControllers();

        OnContextReady();

        _app.Run();
    }

    /// <summary>
    /// Called when the context is ready, but before the app starts running.
    /// </summary>
    public static void OnContextReady()
    {
        using (var scope = _app.Services.CreateScope())
        {
            // Ensure the database is created.
            var db = scope.ServiceProvider.GetRequiredService<HiveMimeContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }
}

