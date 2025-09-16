using Microsoft.EntityFrameworkCore;

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
    }
}