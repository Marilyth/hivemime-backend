public class CreatePostDto
{
    public Guid? HiveId { get; set; }
    public List<CreatePollDto> Polls { get; set; }
}
