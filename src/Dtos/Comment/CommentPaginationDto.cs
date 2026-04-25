public class CommentPaginationDto
{
    public int? Cursor { get; set; }
    public string? Filter { get; set; }
    public CommentOrderBy OrderBy { get; set; }
    public int PageSize { get; set; } = 20;
}

public enum CommentOrderBy
{
    New,
    Old,
    Best
}