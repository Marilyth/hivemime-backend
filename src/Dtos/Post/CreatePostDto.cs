public class CreatePostDto
{
    public int? HiveId { get; set; }
    
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<CreatePollDto> Polls { get; set; }
}
