public class HivePaginationDto : PaginationDto
{
    public string? Filter { get; set; }
    public HiveOrderBy OrderBy { get; set; }
}

public enum HiveOrderBy
{
    New,
    Old,
    Users
}