using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
        DotNetEnv.Env.TraversePath().Load();
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        // Add services to the container.
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.UseOneOfForPolymorphism();
            o.UseAllOfForInheritance();
        });
        services.AddResponseCompression();
        services.AddRequestDecompression();
        services.AddControllers()
            .AddMvcOptions(o => o.Filters.Add(new AuthorizeFilter()))
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new CandidateVoteDtoConverter());
                options.JsonSerializerOptions.Converters.Add(new VoteQueryConverter());
            });

        services.AddDbContextFactory<HiveMimeContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)), ServiceLifetime.Scoped);
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

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(GetUserIdentifier(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1)
                })
            );

            options.AddPolicy("5/1s", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetUserIdentifier(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromSeconds(1)
                })
            );

            options.AddPolicy("5/5s", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetUserIdentifier(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromSeconds(5)
                })
            );

            options.AddPolicy("1/1s", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetUserIdentifier(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(1)
                })
            );

            options.AddPolicy("1/5s", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetUserIdentifier(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(5)
                })
            );

            options.AddPolicy("1/1m", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetUserIdentifier(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromMinutes(1)
                })
            );

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers["Retry-After"] =
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ?
                        retryAfter.TotalSeconds.ToString() :
                        "60";

                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
            };
        });
        
        string observabilityEndpointString = builder.Configuration["Observability:Endpoint"];
        if (!string.IsNullOrEmpty(observabilityEndpointString))
        {
            var observabilityEndpoint = new Uri(observabilityEndpointString);
            builder.Logging.AddOpenTelemetry();

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("HiveMime-Backend"))
                .WithTracing(t => t
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = observabilityEndpoint;
                    }))
                .WithMetrics(m => m
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = observabilityEndpoint;
                    }))
                .WithLogging(l => l
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = observabilityEndpoint;
                    }));
        }

        // In case we ever decide to use Redis, use HybridCache where it makes sense.
        services.AddHybridCache(o => o.DefaultEntryOptions = new()
        {
            Expiration = TimeSpan.FromMinutes(5)
        });

        // Add custom services.
        services.AddMemoryCache();
        services.AddScoped<PostService>();
        services.AddScoped<PostVoteService>();
        services.AddScoped<PostResultService>();
        services.AddScoped<UserService>();
        services.AddScoped<HiveService>();
        services.AddScoped<CommentService>();
        services.AddScoped<AuthorizationService>();
        services.AddScoped<HoneyDeltaCalculator>();
        services.AddScoped(s => s.GetService<IHttpContextAccessor>().HttpContext.User);
        services.AddSingleton<GeoIPService>();
        services.AddSingleton<HotnessUpdateQueue>();
        services.AddSingleton<IMediaService, CloudflareR2Service>();
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

        _app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = error switch
                {
                    ValidationException => 400,
                    UnauthorizedAccessException => 401,
                    NotFoundException => 404,
                    _ => 500
                };

                await context.Response.WriteAsJsonAsync(new
                {
                    error = error?.Message
                });
            });
        });

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
        _app.UseRateLimiter();
        
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
            var db = scope.ServiceProvider.GetRequiredService<HiveMimeContext>();
            db.Database.Migrate();
        }
    }

    private static string GetUserIdentifier(HttpContext context)
    {
        return $"{(context.User.Identity.Name ??
            context.Connection.RemoteIpAddress?.ToString() ??
            "unknown")}_{context.Request.Path}";
    }
}

