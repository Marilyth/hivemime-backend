public class DivisionSearchResultDto
{
    public Guid Id { get; set; }
    
    public string LocalName { get; set; }
    public string? EnglishName { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }

    public int? AdminLevel { get; set; }
    public int? Population { get; set; }
    
    public DivisionSubType Subtype { get; set; }
    public DivisionClass? Class { get; set; }

    public string GeometryGeoJSON { get; set; }
}