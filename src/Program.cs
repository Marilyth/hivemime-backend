using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

public class Program
{
    public static readonly TokenValidationParameters Parameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = "https://securetoken.google.com/hivemime-6072d",
        ValidateAudience = true,
        ValidAudience = "hivemime-6072d",
        ValidateLifetime = true
    };

    private static WebApplication _app;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        // Add services to the container.
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddResponseCompression();
        services.AddRequestDecompression();
        services.AddControllers()
            .AddMvcOptions(o => o.Filters.Add(new AuthorizeFilter()))
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddDbContextFactory<HiveMimeContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default")), ServiceLifetime.Scoped);
        services.AddScoped(s => s.GetService<IDbContextFactory<HiveMimeContext>>().CreateDbContext());

        services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.Authority = Parameters.ValidIssuer;
                options.TokenValidationParameters = Parameters;
            });
        services.AddAuthorization();

        services.AddSwaggerGen(setup =>
        {
            setup.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token.",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer",
                }
            );

            setup.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
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
        services.AddScoped<PostService>();
        services.AddScoped<UserService>();
        services.AddScoped<HiveService>();
        services.AddScoped<CommentService>();
        services.AddScoped(s => s.GetService<IHttpContextAccessor>().HttpContext.User);
        services.AddSingleton<GeoIPService>();
        services.AddSingleton<HotnessUpdateQueue>();
        services.AddHttpClient();
        
        // Add ITriggers.
        foreach (var type in typeof(Program).Assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract && typeof(ITrigger).IsAssignableFrom(type))
                services.AddScoped(typeof(ITrigger), type);
        }

        services.AddScoped<TriggerDispatcher>();

        // Add workers.
        services.AddHostedService<HotnessUpdater>();

        // Configure Mapster.
        MapsterConfiguration.Configure();

        _app = builder.Build();

        // Make IP retrieval work for reverse proxies.
        _app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        _app.UseCors("AllowAll");
        _app.UseResponseCompression();

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
            // During development, reset the databse on restart.
            var db = scope.ServiceProvider.GetRequiredService<HiveMimeContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }
    }
}

