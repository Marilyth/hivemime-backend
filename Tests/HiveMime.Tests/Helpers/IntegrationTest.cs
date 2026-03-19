using Microsoft.Extensions.DependencyInjection;

namespace HiveMime.Tests;

[CollectionDefinition("Database")]
public abstract class DatabaseCollection : ICollectionFixture<DatabaseContainer> { }

[Collection("Database")]
public abstract class IntegrationTest : IDisposable
{
    private DatabaseContainer _fixture;
    private TestWebApplicationFactory _factory;
    private IServiceScope _scope;

    public IntegrationTest(DatabaseContainer fixture)
    {
        _fixture = fixture;
        _factory = new TestWebApplicationFactory(_fixture.ConnectionString);

        _scope = _factory.Services.CreateScope();
        Context = _scope.ServiceProvider.GetRequiredService<HiveMimeContext>();
        Context.Database.EnsureCreated();

        SeedDatabase();
        Context.SaveChanges();
    }

    protected HiveMimeContext Context { get; private set; }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        _scope.Dispose();
        _factory.Dispose();
    }

    protected virtual void SeedDatabase() {}
}