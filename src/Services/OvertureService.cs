
using System.Data.Common;
using DuckDB.NET.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;

/// <summary>
/// A service responsible to fetching relevant geojson files and metadata for regions on the world.
/// </summary>
public class OvertureService(HiveMimeContext context, ILogger<OvertureService> logger, WKBReader reader)
{
    private static bool _hasDoneSetup = false;
    private DuckDBConnection _connection = new DuckDBConnection($"Data Source=:memory:");

    public async Task PopulateDatabaseAsync()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
            return;

        await _connection.OpenAsync();

        if (_hasDoneSetup)
            return;

        // Need spatial and httpfs extensions for overture.
        var installCommand = _connection.CreateCommand();
        installCommand.CommandText = """
            INSTALL SPATIAL;
            INSTALL 'httpfs';
            LOAD spatial;
            LOAD httpfs;
            SET s3_region='eu-central-1';
            """;

        await installCommand.ExecuteNonQueryAsync();
        await DownloadDivisionDataAsync();
        await DownloadDivisionAreaDataAsync();

        _hasDoneSetup = true;
    }
    
    private async Task<Command> CreateCommandAsync(string location)
    {
        await PopulateDatabaseAsync();
        return new Command(_connection, location);
    }

    private async Task DownloadDivisionDataAsync()
    {
        string fileName = "divisions/division/data.parquet";

        var downloadCommand = await CreateCommandAsync(fileName);
        downloadCommand.Select("*");
        await downloadCommand.DownloadAsync();

        if (await context.Divisions.AnyAsync())
            return;

        // Insert without references in the first go.
        int inserted = 0;
        int offset = 0;
        while (true)
        {
            var command = await CreateCommandAsync(fileName);
            command.Select(
                "id",
                "names.primary AS local_name",
                "names.common['en'] AS english_name",
                "country",
                "region",
                "admin_level",
                "population",
                "subtype",
                "class",
                "ST_AsWKB(geometry) AS geometry");
            command.AdditionalQuery($"LIMIT 10000 OFFSET {offset}");

            List<Division> divisions = command.Get(row => new Division
            {
                Id = Guid.Parse(row.GetString(row.GetOrdinal("id"))),
                LocalName = row.GetString(row.GetOrdinal("local_name")),
                EnglishName = GetNullableString(row, "english_name"),
                Country = GetNullableString(row, "country"),
                Region = GetNullableString(row, "region"),
                AdminLevel = GetNullableInt32(row, "admin_level"),
                Population = GetNullableInt32(row, "population"),
                Subtype = Enum.Parse<DivisionSubType>(row.GetString(row.GetOrdinal("subtype")), true),
                Class = GetNullableEnum<DivisionClass>(row, "class"),
                Geometry = reader.Read(row.GetStream(row.GetOrdinal("geometry")))
            });

            if (divisions.Count == 0)
                break;

            await SaveDivisionsAsync(divisions, inserted);
            inserted += divisions.Count;
            offset += divisions.Count;
        }

        // Connect the references in the second go.
        inserted = 0;
        offset = 0;
        while (true)
        {
            var command = await CreateCommandAsync(fileName);
            command.Select("id", "capital_division_ids", "parent_division_id");
            command.AdditionalQuery($"LIMIT 10000 OFFSET {offset}");

            List<Division> divisions = command.Get(row =>
            {
                int capitalIds = row.GetOrdinal("capital_division_ids");
                return new Division
                {
                    Id = Guid.Parse(row.GetString(row.GetOrdinal("id"))),
                    ParentId = GetNullableGuid(row, "parent_division_id"),
                    Capitals = row.IsDBNull(capitalIds)
                        ? null
                        : row.GetFieldValue<List<string>>(capitalIds)
                            .Select(id => new Division { Id = Guid.Parse(id) })
                            .ToList()
                };
            });

            if (divisions.Count == 0)
                break;

            await SaveDivisionRelationshipsAsync(divisions, inserted);
            inserted += divisions.Count;
            offset += divisions.Count;
        }
    }

    private async Task SaveDivisionsAsync(List<Division> divisions, int offset)
    {
        logger.LogInformation("Inserting divisions {Offset} to {End}", offset, offset + divisions.Count);
        await context.Divisions.AddRangeAsync(divisions);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private async Task SaveDivisionRelationshipsAsync(List<Division> divisions, int offset)
    {
        logger.LogInformation("Inserting division relationships {Offset} to {End}", offset, offset + divisions.Count);

        // The references must be equal for all divisions in the same batch.
        Dictionary<Guid, Division> stubs = divisions.ToDictionary(d => d.Id);
        foreach (var division in divisions)
        {
            if (division.Capitals is null)
                continue;

            for (int c = 0; c < division.Capitals.Count; c++)
            {
                if (!stubs.TryGetValue(division.Capitals[c].Id, out var stub))
                    stubs[division.Capitals[c].Id] = division.Capitals[c];
                else
                    division.Capitals[c] = stub;
            }
        }

        context.Divisions.AttachRange(divisions);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private async Task DownloadDivisionAreaDataAsync()
    {
        string fileName = "divisions/division_area/data.parquet";

        var command = await CreateCommandAsync(fileName);
        command.Select("*");

        await command.DownloadAsync();

        if (await context.DivisionAreas.AnyAsync())
            return;

        int inserted = 0;
        int offset = 0;
        while (true)
        {
            command = await CreateCommandAsync(fileName);
            command.Select("id", "class", "division_id", "ST_AsWKB(geometry) AS geometry");
            command.AdditionalQuery($"LIMIT 10000 OFFSET {offset}");

            List<DivisionArea> divisionAreas = command.Get(row => new DivisionArea
            {
                Id = Guid.Parse(row.GetString(row.GetOrdinal("id"))),
                AreaClass = Enum.Parse<AreaClass>(row.GetString(row.GetOrdinal("class")), true),
                DivisionId = Guid.Parse(row.GetString(row.GetOrdinal("division_id"))),
                Geometry = reader.Read(row.GetStream(row.GetOrdinal("geometry")))
            });

            if (divisionAreas.Count == 0)
                break;

            await SaveDivisionAreasAsync(divisionAreas, inserted);
            inserted += divisionAreas.Count;
            offset += divisionAreas.Count;
        }
    }

    private async Task SaveDivisionAreasAsync(List<DivisionArea> divisionAreas, int offset)
    {
        logger.LogInformation("Inserting division areas {Offset} to {End}", offset, offset + divisionAreas.Count);
        await context.DivisionAreas.AddRangeAsync(divisionAreas);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static string? GetNullableString(DbDataReader row, string column)
    {
        int ordinal = row.GetOrdinal(column);
        return row.IsDBNull(ordinal) ? null : row.GetString(ordinal);
    }

    private static int? GetNullableInt32(DbDataReader row, string column)
    {
        int ordinal = row.GetOrdinal(column);
        return row.IsDBNull(ordinal) ? null : row.GetInt32(ordinal);
    }

    private static Guid? GetNullableGuid(DbDataReader row, string column)
    {
        int ordinal = row.GetOrdinal(column);
        return row.IsDBNull(ordinal) ? null : Guid.Parse(row.GetString(ordinal));
    }

    private static T? GetNullableEnum<T>(DbDataReader row, string column) where T : struct, Enum
    {
        string? value = GetNullableString(row, column);
        return value is null ? null : Enum.Parse<T>(value, true);
    }

    private class Command(DuckDBConnection connection, string location)
    {
        private List<string> _selects = [];
        private string _additionalSyntax;

        /// <summary>
        /// Selects the columns to return.
        /// For a list of columns, see https://docs.overturemaps.org/schema/
        /// </summary>
        /// <param name="selects">The columns to select.</param>
        public Command Select(params string[] selects)
        {
            _selects.AddRange(selects);
            return this;
        }

        public Command AdditionalQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(_additionalSyntax))
                _additionalSyntax = query;
            else
                _additionalSyntax += $"\n{query}";

            return this;
        }

        public async Task DownloadAsync(bool overwrite = false)
        {
            var command = connection.CreateCommand();
            string query = GetCommandText();

            string[] fileSegments = location.Split('/');

            if (fileSegments.Length < 3)
                throw new ArgumentException("Invalid location format. Expected format: theme/type/fileName");

            // The file already exists, don't download it again.
            if (File.Exists(location) && !overwrite)
               return;

            // Create the local directory if it doesn't exist.
            string directory = Path.GetDirectoryName(location);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string remoteLocation = $"s3://overturemaps-us-west-2/release/2026-07-22.0/theme={fileSegments[0]}/type={fileSegments[1]}/*";
            query = query.Replace(location, remoteLocation);

            command.CommandText = $"""
            COPY ( {query} ) TO '{location}' (FORMAT PARQUET);
            """;

            await command.ExecuteNonQueryAsync();
        }

        public List<T> Get<T>(Func<DbDataReader, T> map)
        {
            var command = connection.CreateCommand();
            command.CommandText = GetCommandText();

            using var result = command.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
            List<T> rows = [];
            while (result.Read())
                rows.Add(map(result));

            return rows;
        }

        private string GetCommandText()
        {
            string query = $"""
            SELECT {string.Join(", ", _selects)}
            FROM read_parquet('{location}', hive_partitioning=1)
            """;

            if (!string.IsNullOrWhiteSpace(_additionalSyntax))
                query += $"\n{_additionalSyntax}";

            return query;
        }
    }
}
