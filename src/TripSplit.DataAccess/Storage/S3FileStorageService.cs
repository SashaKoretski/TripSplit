using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.DataAccess.Infrastructure.Exceptions;

namespace TripSplit.DataAccess.Storage;

// Реализация файлового хранилища поверх S3-совместимого API (AWS S3, MinIO и т.п.)
public sealed class S3FileStorageService : IFileStorageService, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly bool _useHttp;

    public S3FileStorageService(S3StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Bucket))
            throw new ArgumentException("Bucket is required", nameof(options));

        _bucket = options.Bucket;
        _useHttp = options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = true,
            UseHttp = _useHttp
        };
        _client = new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        try
        {
            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false
            }, ct);
        }
        catch (AmazonS3Exception e)
        {
            throw new StorageException($"Failed to upload '{key}': {e.Message}", e);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteObjectAsync(_bucket, key, ct);
        }
        catch (AmazonS3Exception e)
        {
            throw new StorageException($"Failed to delete '{key}': {e.Message}", e);
        }
    }

    public async Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            return await _client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Expires = DateTime.UtcNow.Add(expiry),
                Verb = HttpVerb.GET,
                Protocol = _useHttp ? Protocol.HTTP : Protocol.HTTPS
            });
        }
        catch (AmazonS3Exception e)
        {
            throw new StorageException($"Failed to presign '{key}': {e.Message}", e);
        }
    }

    public void Dispose() => _client.Dispose();
}
