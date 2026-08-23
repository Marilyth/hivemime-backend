using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class GeoController(GeoService geoService) : ControllerBase
{
    [HttpGet("searchDivisions")]
    [EnableRateLimiting("5/1s")]
    [AllowAnonymous]
    public async Task<List<DivisionSearchResultDto>> SearchDivisions(string query)
        => await geoService.SearchDivisionAsync(query);

    [HttpGet("searchDivisionsByBbox")]
    [EnableRateLimiting("5/1s")]
    [AllowAnonymous]
    public async Task<List<DivisionSearchResultWithAreaDto>> SearchDivisionsByBbox(double minX, double minY, double maxX, double maxY)
        => await geoService.SearchDivisionByBboxAsync(minX, minY, maxX, maxY);
}