using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

/// <summary>
/// A service responsible to fetching relevant geojson files and metadata for regions on the world.
/// </summary>
public class GeoService
{
    private readonly HiveMimeContext _context;
    private readonly GeoJsonWriter _writer;

    public GeoService(HiveMimeContext context, GeoJsonWriter writer, OvertureService overture)
    {
        _context = context;
        _writer = writer;

        // Ensure the database is populated. Can't continue otherwise.
        overture.PopulateDatabaseAsync().Wait();
    }

    public async Task<List<DivisionSearchResultDto>> SearchDivisionAsync(string name)
    {
        var results = await _context.Divisions
            .AsNoTracking()
            .Where(d => EF.Functions.ILike(d.LocalName, $"%{name}%") || EF.Functions.ILike(d.EnglishName, $"%{name}%"))
            .OrderByDescending(d =>
                EF.Functions.ILike(d.LocalName, name) ||
                EF.Functions.ILike(d.EnglishName, name))
            .ThenByDescending(d =>
                EF.Functions.ILike(d.LocalName, $"{name}%") ||
                EF.Functions.ILike(d.EnglishName, $"{name}%"))
            .ThenBy(d => d.AdminLevel)
            .ThenBy(d => d.Subtype)
            .ThenBy(d => d.Class)
            .ThenByDescending(d => d.Population)
            .Take(25)
            .ToListAsync();

        return results.Select(d => new DivisionSearchResultDto
        {
            Id = d.Id,
            LocalName = d.LocalName,
            EnglishName = d.EnglishName,
            Subtype = d.Subtype,
            Class = d.Class,
            AdminLevel = d.AdminLevel,
            Country = d.Country,
            GeometryGeoJSON = _writer.Write(d.Geometry),
            Population = d.Population,
            Region = d.Region
        }).ToList();
    }

    public async Task<List<DivisionSearchResultWithAreaDto>> SearchDivisionByBboxAsync(double minX, double minY, double maxX, double maxY)
    {
        var polygon = GeometryFactory.Default.CreatePolygon([
            new Coordinate(minX, minY),
            new Coordinate(maxX, minY),
            new Coordinate(maxX, maxY),
            new Coordinate(minX, maxY),
            new Coordinate(minX, minY)
        ]);

        // ToDo: Update to use postgis's && operator for geometry in dotnet 11 for much improved performance. https://github.com/npgsql/efcore.pg/pull/3484/changes
        // ToDo: Combine subtype and geometry indexes somehow for ordering by subtype. https://www.postgresql.org/docs/current/btree-gist.html
        var results = await _context.Divisions
            .AsNoTracking()
            .AsSingleQuery()
            .Include(d => d.Areas.Where(a => a.AreaClass == AreaClass.Land))
            .Where(d => d.Areas.Any(a => a.Geometry.Intersects(polygon)))
            .Take(50)
            .ToListAsync();

        return results.Select(d => new DivisionSearchResultWithAreaDto
        {
            Id = d.Id,
            LocalName = d.LocalName,
            EnglishName = d.EnglishName,
            Subtype = d.Subtype,
            Class = d.Class,
            AdminLevel = d.AdminLevel,
            Country = d.Country,
            GeometryGeoJSON = _writer.Write(d.Geometry),
            Population = d.Population,
            Region = d.Region,
            Areas = d.Areas.Select(a => new DivisionAreaSearchResultDto
            {
                Id = a.Id,
                AreaClass = a.AreaClass
            })
        }).ToList();
    }
}
