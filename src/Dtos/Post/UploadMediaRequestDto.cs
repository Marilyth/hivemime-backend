public class UploadMediaRequestDto
{
    public string ContentType { get; set; }
    public ulong ContentLength { get; set; }
    public ulong ThumbnailContentLength { get; set; }
}