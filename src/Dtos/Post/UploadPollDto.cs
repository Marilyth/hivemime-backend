public class UploadPollDto
{
    public int Id { get; set; }
    public List<string> MediaUploadUrls { get; set; }
    public List<UploadCandidateDto> Candidates { get; set; }
}