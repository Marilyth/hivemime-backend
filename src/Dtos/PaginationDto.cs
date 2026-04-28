public abstract class PaginationDto
{
    public int PageSize { get; set; } = 20;
    public PaginationCursorDto? Cursor { get; set; }
}