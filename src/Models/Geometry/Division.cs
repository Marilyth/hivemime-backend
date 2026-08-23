using NetTopologySuite.Geometries;

public class Division : Entity
{
    public string LocalName { get; set; }
    public string? EnglishName { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }

    public int? AdminLevel { get; set; }
    public int? Population { get; set; }
    
    public DivisionSubType Subtype { get; set; }
    public DivisionClass? Class { get; set; }
    
    public List<Division> Children { get; set; }
    public Division? Parent { get; set; }
    public Guid? ParentId { get; set; }

    public List<Division> Capitals { get; set; }
    public List<Division> CapitalOf { get; set; }

    public List<DivisionArea> Areas { get; set; }

    public Geometry Geometry { get; set; }
}

public enum DivisionClass
{
    Megacity,
    City,
    Town,
    Village,
    Hamlet
}

public enum DivisionSubType
{
    Country,
    Dependency,
    Macroregion,
    Region,
    Macrocounty,
    County,
    Localadmin,
    Locality,
    Borough,
    Macrohood,
    Neighborhood,
    Microhood
}