public interface IMediaService
{
    string GetPreSignedURL(string objectKey, ulong contentLength, string contentType);
    Task<List<string>> ListObjectsAsync(string prefix);
    Task DeleteObjectsAsync(string prefix);
}
