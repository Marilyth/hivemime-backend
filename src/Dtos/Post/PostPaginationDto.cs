public class PostPaginationDto : PaginationDto
{
    public string? Filter { get; set; }
    public PostOrderBy OrderBy { get; set; }
}

public enum PostOrderBy
{
    New,
    Old,
    Hot
}