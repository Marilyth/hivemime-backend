public class UploadPollDto
{
    public int Id { get; set; }
    public List<string> MediaUploadUrls { get; set; }
    public List<UploadCandidateDto> Candidates { get; set; }
}

public class UploadPollRequestDto
{
    public int Id { get; set; }
    public UploadMediaRequestDto Media { get; set; }
    public List<UploadCandidateRequestDto> Candidates { get; set; }
}