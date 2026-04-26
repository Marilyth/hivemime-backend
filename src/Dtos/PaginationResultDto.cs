public class PaginationResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int? NextCursor { get; set; }
}