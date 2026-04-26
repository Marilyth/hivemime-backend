public class CommentPaginationDto : PaginationDto
{
    public string? Filter { get; set; }
    public CommentOrderBy OrderBy { get; set; }
}

public enum CommentOrderBy
{
    New,
    Old,
    Best
}