using Amazon.S3;
using Amazon.S3.Model;
using Moq;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Storage;
using Xunit;

namespace TripSplit.DataAccess.Tests.Storage;

/// <summary>
/// Unit-тесты S3FileStorageService: реальный AWS S3/MinIO клиент подменен моком IAmazonS3
/// (London-стиль с Moq), сеть не используется. Внутренний конструктор, принимающий
/// IAmazonS3, добавлен специально для тестируемости (см. S3FileStorageService.cs) —
/// без него Act нельзя было вызвать в изоляции от реального сетевого клиента (Т5 CLAUDE_TEST).
/// </summary>
public sealed class S3FileStorageServiceTests
{
    private readonly Mock<IAmazonS3> _client = new();

    private S3FileStorageService CreateSut() => new(_client.Object, "tripsplit-receipts", useHttp: true);

    [Fact]
    public async Task UploadAsync_Valid_CallsPutObjectWithBucketAndKey()
    {
        var sut = CreateSut();
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });

        await sut.UploadAsync("receipts/x.jpg", content, "image/jpeg");

        _client.Verify(c => c.PutObjectAsync(
            It.Is<PutObjectRequest>(r => r.BucketName == "tripsplit-receipts" && r.Key == "receipts/x.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_S3Throws_WrapsIntoStorageException()
    {
        _client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("bucket unreachable"));
        var sut = CreateSut();
        using var content = new MemoryStream(new byte[] { 1 });

        await Assert.ThrowsAsync<StorageException>(() => sut.UploadAsync("k", content, "image/jpeg"));
    }

    [Fact]
    public async Task DeleteAsync_Valid_CallsDeleteObjectWithBucketAndKey()
    {
        var sut = CreateSut();

        await sut.DeleteAsync("receipts/x.jpg");

        _client.Verify(c => c.DeleteObjectAsync("tripsplit-receipts", "receipts/x.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_S3Throws_WrapsIntoStorageException()
    {
        _client.Setup(c => c.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("not found"));
        var sut = CreateSut();

        await Assert.ThrowsAsync<StorageException>(() => sut.DeleteAsync("k"));
    }

    [Fact]
    public async Task GetPresignedUrlAsync_Valid_ReturnsUrlFromClient()
    {
        _client.Setup(c => c.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync("https://minio/presigned");
        var sut = CreateSut();

        var url = await sut.GetPresignedUrlAsync("k", TimeSpan.FromMinutes(5));

        Assert.Equal("https://minio/presigned", url);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_S3Throws_WrapsIntoStorageException()
    {
        _client.Setup(c => c.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ThrowsAsync(new AmazonS3Exception("denied"));
        var sut = CreateSut();

        await Assert.ThrowsAsync<StorageException>(() => sut.GetPresignedUrlAsync("k", TimeSpan.FromMinutes(5)));
    }
}
