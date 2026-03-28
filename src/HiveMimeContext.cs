using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

public class HiveMimeContext : DbContext
{
    public HiveMimeContext(DbContextOptions<HiveMimeContext> options) : base(options) { }

    public DbSet<Hive> Hives { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<Candidate> PollOptions { get; set; }
    public DbSet<PostVote> PostVotes { get; set; }
    public DbSet<CandidateVote> CandidateVotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define relationships and constraints here if needed.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            SetEntityRules(entity);
        }

        modelBuilder.Entity<User>()
            .HasMany(u => u.CreatedHives)
            .WithOne(h => h.Creator);

        modelBuilder.Entity<Hive>()
            .HasMany(h => h.Followers)
            .WithMany(u => u.FollowedHives);
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