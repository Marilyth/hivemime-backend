public class HiveUserPaginationDto : PaginationDto
{
    public string? Filter { get; set; }
    public HiveUserOrderBy OrderBy { get; set; }
}

public enum HiveUserOrderBy
{
    New,
    Old
}