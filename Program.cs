using Microsoft.EntityFrameworkCore;

public class Program
{
    private static WebApplication _app;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();
        builder.Services.AddDbContext<HiveMimeContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("TestConnection")));

        _app = builder.Build();

        // Configure the HTTP request pipeline.
        if (_app.Environment.IsDevelopment())
        {
            _app.UseSwagger();
            _app.UseSwaggerUI();
        }

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

