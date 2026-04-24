using MaxMind.GeoIP2;

public class GeoIPService(IHttpClientFactory httpClientFactory)
{
    private DatabaseReader _dbReader;
    private const string CountryDownloadUri = "https://git.io/GeoLite2-Country.mmdb";
    private const string CountryDbPath = "GeoLite2-Country.mmdb";
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public async Task<string?> GetCountryOfIPAsync(string ipAddress)
    {
        await DownloadCountryDatabaseAsync();
        string? country = null;

        try { country = _dbReader?.Country(ipAddress).Country.IsoCode; }
        catch { }
        
        return country;
    }

    private async Task DownloadCountryDatabaseAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (!File.Exists(CountryDbPath) || (DateTime.UtcNow - File.GetLastWriteTimeUtc(CountryDbPath)).TotalDays >= 30)
            {
                using HttpClient client = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await client.GetAsync(CountryDownloadUri);
                response.EnsureSuccessStatusCode();

                await using FileStream fs = new(CountryDbPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                _dbReader?.Dispose();
                _dbReader = null;
            }

            if (_dbReader is null)
                _dbReader = new DatabaseReader(CountryDbPath);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}