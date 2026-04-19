public class CreatePostDto
{
    public int? HiveId { get; set; }
    public List<CreatePollDto> Polls { get; set; }
}
