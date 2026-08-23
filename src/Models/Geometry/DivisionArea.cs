using NetTopologySuite.Geometries;

public class DivisionArea : Entity
{
    public AreaClass AreaClass { get; set; }

    public Division Division { get; set; }
    public Guid DivisionId { get; set; }

    public Geometry Geometry { get; set; }
}

public enum AreaClass
{
    Land,
    Maritime
}