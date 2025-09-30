public static class CategoryExtensions
{
    public static Category ToCategory(this PollCategoryDto option)
    {
        return new Category
        {
            Name = option.Name,
            Description = string.Empty
        };
    }

    public static Category ToCategory(this string option)
    {
        return new Category
        {
            Name = option,
            Description = string.Empty
        };
    }

    public static PollCategoryDto ToCategoryDto(this Category option)
    {
        return new PollCategoryDto
        {
            Id = option.Id,
            Name = option.Name,
            Description = option.Description ?? string.Empty,
        };
    }
}