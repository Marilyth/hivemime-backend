public class UploadPostDto
{
    public int Id { get; set; }
    public List<UploadPollDto> Polls { get; set; }
}

public class UploadPostRequestDto
{
    public int Id { get; set; }
    public List<UploadPollRequestDto> Polls { get; set; }
}

public class UploadMediaRequestDto
{
    public string ContentType { get; set; }
    public ulong ContentLength { get; set; }
    public ulong ThumbnailContentLength { get; set; }
}