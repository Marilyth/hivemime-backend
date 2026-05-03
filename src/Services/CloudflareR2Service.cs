using Amazon.S3;
using Amazon.S3.Model;

public class CloudflareR2Service
{
    public const ulong MaxFileSize = 1024 * 1024; // 1 MB
    public const ulong MaxTotalSize = 10 * 1024 * 1024; // 10 MB

    private readonly IAmazonS3 _amazonS3;

    public CloudflareR2Service(IConfiguration configuration)
    {
        var accessKey = configuration["R2:AccessKey"];
        var secretKey = configuration["R2:SecretKey"];
        var serviceUrl = configuration["R2:S3API"];

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };

        _amazonS3 = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<List<S3Object>> ListObjectsAsync(string prefix)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = "HiveMime",
            Prefix = prefix
        };

        var response = await _amazonS3.ListObjectsV2Async(request);
        return response.S3Objects;
    }

    public string GetPreSignedURL(string objectKey, ulong contentLength, string contentType)
    {
        if (string.IsNullOrEmpty(objectKey))
            throw new ArgumentException("Object key cannot be null or empty.", nameof(objectKey));

        if (contentLength <= 0)
            throw new ArgumentException("Content length must be greater than zero.", nameof(contentLength));

        if (contentLength > MaxFileSize)
            throw new ArgumentException($"Content length cannot exceed {MaxFileSize} bytes.", nameof(contentLength));

        if (string.IsNullOrEmpty(contentType))
            throw new ArgumentException("Content type cannot be null or empty.", nameof(contentType));

        string extension = MimeTypeToExtension(contentType);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = "HiveMime",
            Key = objectKey + extension,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(TimeSpan.FromMinutes(5))
        };

        request.Headers["Content-Type"] = contentType;
        request.Headers["Content-Length"] = contentLength.ToString();

        return _amazonS3.GetPreSignedURL(request);
    }

    public string MimeTypeToExtension(string mimeType)
    {
        return mimeType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            _ => throw new ValidationException($"Unsupported MIME type for media upload ({mimeType})")
        };
    }
}