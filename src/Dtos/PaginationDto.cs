public class PostPaginationDto
{
    public CursorDto? Cursor { get; set; }
    public OrderBy OrderBy { get; set; }
    public bool Ascending { get; set; }
    public int PageSize { get; set; } = 20;
}

public class CursorDto
{
    public int AfterId { get; set; }
    public string Cursor { get; set; }
}

public enum OrderBy
{
    DateCreated,
    Hotness
}