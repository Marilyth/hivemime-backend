public class ListPollDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public PollAnswerType PollType { get; set; }
    public List<PollOptionDto> Options { get; set; }
}