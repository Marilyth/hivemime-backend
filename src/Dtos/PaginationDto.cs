public class PostPaginationDto
{
    public int? Cursor { get; set; }
    public OrderBy OrderBy { get; set; }
    public int PageSize { get; set; } = 20;
}

public enum OrderBy
{
    New,
    Old,
    Hot
}