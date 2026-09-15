using System.IO;
using System.Threading;

namespace TripSplit.BusinessLogic.Interfaces.Services;

// Абстракция над объектным хранилищем (S3-совместимым)
public interface IFileStorageService
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
}
