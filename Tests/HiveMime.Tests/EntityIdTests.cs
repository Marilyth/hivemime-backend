using Microsoft.EntityFrameworkCore;

namespace HiveMime.Tests;

public class EntityIdTests : IntegrationTest
{
    public EntityIdTests(DatabaseContainer fixture) : base(fixture) { }

    [Fact]
    public async Task SaveChangesAsync_EntityId_IsSameBeforeAndAfterCreation()
    {
        // Arrange
        var user = new User { Username = "id-test-user", Settings = new() };
        var idBeforeSave = user.Id;

        Context.Users.Add(user);

        // Act
        await Context.SaveChangesAsync();
        var persistedId = await Context.Users
            .Where(u => u.Id == idBeforeSave)
            .Select(u => u.Id)
            .SingleOrDefaultAsync();

        // Assert
        Assert.Equal(idBeforeSave, user.Id);
        Assert.Equal(idBeforeSave, persistedId);
    }
}
