public class UploadPollDto
{
    public Guid Id { get; set; }
    public List<string> MediaUploadUrls { get; set; }
    public List<UploadCandidateDto> Candidates { get; set; }
}