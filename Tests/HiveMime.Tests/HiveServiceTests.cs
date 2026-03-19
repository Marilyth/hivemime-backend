using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class HiveServiceTests : IntegrationTest
{
    private IHiveService _service;
    private Hive? _defaultHive;

    public HiveServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<IHiveService>();
    }

    [Fact]
    public void GetHiveById_WithValidId_ReturnsHive()
    {
        // Act
        var result = _service.GetHiveById(_defaultHive!.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_defaultHive.Id, result.Id);
        Assert.Equal("Default Hive", result.Name);
    }

    [Fact]
    public void BrowseHives_WithHives_ReturnsHives()
    {
        // Act
        var result = _service.BrowseHives(null, "");

        // Assert
        Assert.Single(result);
        Assert.Equal(_defaultHive!.Id, result[0].Id);
        Assert.Equal("Default Hive", result[0].Name);
    }

    [Fact]
    public void CreateHive_ValidHive_AddsToDatabase()
    {
        // Arrange
        var user = Context.Users.First();
        var hiveDto = new CreateHiveDto
        {
            Name = "New Hive",
            Description = "New hive description"
        };
        var service = _service;

        // Act
        var result = service.CreateHive(user.Id, hiveDto);
        var hive = Context.Hives.FirstOrDefault(h => h.Name == "New Hive");

        // Assert
        Assert.NotNull(hive);
        Assert.Equal("New Hive", hive.Name);
        Assert.Equal("New hive description", hive.Description);
        Assert.Equal(user.Id, hive.CreatorId);
    }

    [Fact]
    public void CreateHive_InvalidName_ThrowsException()
    {
        // Arrange
        var user = Context.Users.First();
        var service = _service;
        var hiveDto = new CreateHiveDto
        {
            Name = "ab",
            Description = "Too short name"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.CreateHive(user.Id, hiveDto));
    }

    [Fact]
    public void CreateHive_DuplicateName_ThrowsException()
    {
        // Arrange
        var user = Context.Users.First();
        var service = _service;
        var hiveDto = new CreateHiveDto
        {
            Name = "Default Hive",
            Description = "Duplicate name"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.CreateHive(user.Id, hiveDto));
    }
    
    protected override void SeedDatabase()
    {
        var user = new User { Username = "defaultuser", Settings = new() };
        Context.Users.Add(user);

        _defaultHive = new Hive
        {
            Name = "Default Hive",
            Description = "This is a default hive.",
            Creator = user,
            Posts = [],
            Followers = [user]
        };
        Context.Hives.Add(_defaultHive);
    }
}
