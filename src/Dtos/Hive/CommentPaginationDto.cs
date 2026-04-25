public class HivePaginationDto
{
    public int? Cursor { get; set; }
    public string? Filter { get; set; }
    public HiveOrderBy OrderBy { get; set; }
    public int PageSize { get; set; } = 20;
}

public enum HiveOrderBy
{
    New,
    Old,
    Followers
}