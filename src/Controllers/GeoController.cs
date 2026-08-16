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
    public async Task<List<Division>> SearchDivisions(string query)
        => await geoService.SearchDivisionAsync(query);
}