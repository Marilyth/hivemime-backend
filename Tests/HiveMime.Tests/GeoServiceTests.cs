using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HiveMime.Tests;

public class GeoServiceTests : IntegrationTest
{
    private readonly GeoService _service;

    public GeoServiceTests(DatabaseContainer fixture) : base(fixture)
    {
        _service = Context.GetService<GeoService>();
    }


    [Fact]
    public async Task SearchDivisionAsync_ReturnsDivision()
    {
        var result = await _service.SearchDivisionAsync("Germany");

        Assert.True(result[0].Subtype == "country");
    }
}
