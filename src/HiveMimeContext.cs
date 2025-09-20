using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

public class HiveMimeContext : DbContext
{
    public HiveMimeContext(DbContextOptions<HiveMimeContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<PollOption> PollOptions { get; set; }
    public DbSet<Vote> Votes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define relationships and constraints here if needed.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            SetEntityRules(entity);
        }
    }

    private void SetEntityRules(IMutableEntityType entityType)
    {
        // Set default string length to 1000 for all string properties.
        foreach (var property in entityType.GetProperties()
            .Where(p => p.ClrType == typeof(string)))
        {
            property.SetMaxLength(1000);
        }
    }
}