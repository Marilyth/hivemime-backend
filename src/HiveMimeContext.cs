using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class HiveMimeContext : DbContext
{
    public HiveMimeContext(DbContextOptions<HiveMimeContext> options) : base(options) { }

    public DbSet<Hive> Hives { get; set; }
    public DbSet<HiveFollower> HiveFollowers { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<Candidate> PollOptions { get; set; }
    public DbSet<PostVote> PostVotes { get; set; }
    public DbSet<CandidateVote> CandidateVotes { get; set; }
    public DbSet<Candidate> Candidates { get; set; }
    public DbSet<Category> Categories { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TriggerDispatcher dispatcher = this.GetService<TriggerDispatcher>();
        dispatcher.RegisterChanges(ChangeTracker);
        
        await dispatcher.DispatchAsync(true);
        var result = await base.SaveChangesAsync(cancellationToken);
        await dispatcher.DispatchAsync(false);

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define relationships and constraints here if needed.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            SetEntityRules(entity);

        modelBuilder.Entity<User>()
            .HasMany(u => u.CreatedHives)
            .WithOne(h => h.Creator);

        modelBuilder.Entity<Hive>()
            .HasMany(h => h.Moderators)
            .WithMany(u => u.ModeratedHives);
    }

    private void SetEntityRules(IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(string))
                property.SetMaxLength(1000);

            else if (property.ClrType == typeof(DateTimeOffset))
                property.SetValueConverter(new ValueConverter<DateTimeOffset, DateTimeOffset>(
                    v => v.ToUniversalTime(),
                    v => v
                ));

            else if (property.ClrType == typeof(DateTimeOffset?))
                property.SetValueConverter(new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
                    v => v.HasValue ? v.Value.ToUniversalTime() : v,
                    v => v
                ));
        }
    }
}