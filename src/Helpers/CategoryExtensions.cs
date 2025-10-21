public static class CategoryExtensions
{
    public static Category ToCategory(this PollCategoryDto option)
    {
        return new Category
        {
            Name = option.Name,
            Description = string.Empty,
            Color = option.Color
        };
    }

    public static PollCategoryDto ToCategoryDto(this Category option)
    {
        return new PollCategoryDto
        {
            Name = option.Name,
            Description = option.Description,
            Color = option.Color
        };
    }
}