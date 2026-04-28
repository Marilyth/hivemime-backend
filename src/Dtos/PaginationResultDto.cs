public class PaginationResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public PaginationCursorDto? NextCursor { get; set; }
}

public class PaginationCursorDto
{
    public string Cursor { get; set; }
    public int Id { get; set; }
}