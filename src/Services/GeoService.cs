
using System.Text.Json;
using DuckDB.NET.Data;

/// <summary>
/// A service responsible to fetching relevant geojson files and metadata for regions on the world.
/// </summary>
public class GeoService(ILogger<GeoService> logger)
{
    private DuckDBConnection _connection = new DuckDBConnection($"Data Source=:memory:");

    public async Task<List<Division>> SearchDivisionAsync(string name)
    {
        var command = await CreateCommandAsync($"divisions/division/*.parquet");
        command.Select("*");
        command.AdditionalQuery($"WHERE names.primary ILIKE '%{name}%' OR names.common.en ILIKE '%{name}%'");
        command.AdditionalQuery($"ORDER BY names.common.en");
        command.AdditionalQuery($"LIMIT 25");

        return await command.GetAsync<List<Division>>();
    }

    public async Task<string> GetDivisionAsync(string id)
    {
        var command = await CreateCommandAsync($"divisions/division_area/*.parquet");
        command.Select("localName", "englishName", "subtype");
        command.AdditionalQuery($"WHERE id = '{id}'");

        return await command.GetAsync();
    }

    private async Task<Command> CreateCommandAsync(string location)
    {
        await SetupAsync();
        return new Command(_connection, location);
    }

    private async Task DownloadDataAsync()
    {
        var command = await CreateCommandAsync("divisions/division/data.parquet");
        command.Select("*");

        await command.DownloadAsync();
    }

    private async Task SetupAsync()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
            return;

        await _connection.OpenAsync();

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
        await DownloadDataAsync();
    }
    
    private class Command(DuckDBConnection connection, string location)
    {
        private List<string> _selects = ["id"];
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

        public async Task<string> GetAsync()
        {
            var command = connection.CreateCommand();
            command.CommandText = GetCommandText();

            command.CommandText = $"""
            SELECT json_group_array(to_json(t))
            FROM ( {command.CommandText} ) t
            """;

            return await command.ExecuteScalarAsync() as string;
        }

        public async Task<T> GetAsync<T>() where T : class
        {
            string json = await GetAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
