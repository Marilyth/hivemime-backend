public class UploadCandidateDto
{
    public int Id { get; set; }
    public List<string> MediaUploadUrls { get; set; }
}

public class UploadCandidateRequestDto
{
    public int Id { get; set; }
    public UploadMediaRequestDto Media { get; set; }
}