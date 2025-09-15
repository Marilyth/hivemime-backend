public class GetPollDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public PollAnswerType PollType { get; set; }
    public List<AnswerOptionDto> Options { get; set; }
}