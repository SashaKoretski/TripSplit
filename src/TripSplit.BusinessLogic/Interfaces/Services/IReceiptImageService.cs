using System.IO;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

public interface IReceiptImageService
{
    Task<ReceiptImage> UploadAsync(Guid receiptId, Stream content, string originalFileName, string contentType, long fileSizeBytes);
    Task<ReceiptImage?> GetByReceiptAsync(Guid receiptId);
    Task<string> GetDownloadUrlAsync(Guid receiptId);
    Task DeleteByReceiptAsync(Guid receiptId);
}
