using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/ReceiptImage - url/urlExpiresAt (правка 2026-10-08 в WebLab#2),
// presigned-ссылка строится заново при каждом ответе (IReceiptImageService.GetDownloadUrlAsync).
public class ReceiptImageDto
{
    public Guid Id { get; init; }
    public Guid ReceiptId { get; init; }
    public required string ContentType { get; init; }
    public long FileSizeBytes { get; init; }
    public required string OriginalFileName { get; init; }
    public DateTime UploadedAt { get; init; }
    public required string Url { get; init; }
    public DateTime UrlExpiresAt { get; init; }

    public static ReceiptImageDto FromDomain(ReceiptImage image, string url, DateTime urlExpiresAt) => new()
    {
        Id = image.Id,
        ReceiptId = image.ReceiptId,
        ContentType = image.ContentType,
        FileSizeBytes = image.FileSizeBytes,
        OriginalFileName = image.OriginalFileName,
        UploadedAt = image.UploadedAt,
        Url = url,
        UrlExpiresAt = urlExpiresAt
    };
}
