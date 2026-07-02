using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HiveMime.Tests;

public class CloudflareR2ServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly CloudflareR2Service _service;

    public CloudflareR2ServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["R2:AccessKey"]).Returns("test-access-key");
        _mockConfig.Setup(c => c["R2:SecretKey"]).Returns("test-secret-key");
        _mockConfig.Setup(c => c["R2:S3API"]).Returns("https://localhost");
        _service = new CloudflareR2Service(_mockConfig.Object);
    }

    [Fact]
    public async Task ListObjectsAsync_WithMock_CallsExpectedMethod()
    {
        // Arrange.
        var _mockS3 = new Mock<IAmazonS3>();
        typeof(CloudflareR2Service)
            .GetField("_amazonS3", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_service, _mockS3.Object);

        var s3Objects = new List<S3Object> { new S3Object { Key = "test" } };

        // Act.
        _mockS3.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response { S3Objects = s3Objects });

        // Assert.
        var result = await _service.ListObjectsAsync("prefix");
        Assert.Single(result);
        Assert.Equal("test", result[0]);
    }

    [Fact]
    public void GetPreSignedURL_InvalidInput_Throws()
    {
        // Act & Assert.
        Assert.Throws<ValidationException>(() => _service.GetPreSignedURL(null, 100, "image/png"));
        Assert.Throws<ValidationException>(() => _service.GetPreSignedURL("key", 0, "image/png"));
        Assert.Throws<ValidationException>(() => _service.GetPreSignedURL("key", CloudflareR2Service.MaxFileSize + 1, "image/png"));
        Assert.Throws<ValidationException>(() => _service.GetPreSignedURL("key", 100, null));
    }

    [Fact]
    public void GetPreSignedURL_ValidInput_ReturnsUrl()
    {
        // Act.
        var url = _service.GetPreSignedURL("key", 100, "image/png");

        // Assert.
        Assert.StartsWith("https://localhost/key.png", url);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/gif", ".gif")]
    [InlineData("image/bmp", ".bmp")]
    [InlineData("image/webp", ".webp")]
    public void MimeTypeToExtension_ValidTypes_ReturnsExpected(string mime, string ext)
    {
        // Act.
        string result = CloudflareR2Service.MimeTypeToExtension(mime);

        // Assert.
        Assert.Equal(ext, result);
    }

    [Fact]
    public void MimeTypeToExtension_InvalidType_ReturnsExpected()
    {
        // Act & Assert.
        Assert.Throws<ValidationException>(() => CloudflareR2Service.MimeTypeToExtension("application/pdf"));
    }
}
