public class PostPaginationDto
{
    public int? Cursor { get; set; }
    public string? Filter { get; set; }
    public PostOrderBy OrderBy { get; set; }
    public int PageSize { get; set; } = 20;
}

public enum PostOrderBy
{
    New,
    Old,
    Hot
}