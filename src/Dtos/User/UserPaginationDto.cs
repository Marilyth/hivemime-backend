public class UserPaginationDto : PaginationDto
{
    public string? Filter { get; set; }
    public UserOrderBy OrderBy { get; set; }
}

public enum UserOrderBy
{
    New,
    Old,
    Honey,
    Name
}