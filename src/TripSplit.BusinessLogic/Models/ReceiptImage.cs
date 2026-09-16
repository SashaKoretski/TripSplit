namespace TripSplit.BusinessLogic.Models;

public class ReceiptImage
{
    public Guid Id { get; init; }
    public Guid ReceiptId { get; init; }
    public string StorageKey { get; init; }
    public string ContentType { get; init; }
    public long FileSizeBytes { get; init; }
    public string OriginalFileName { get; init; }
    public DateTime UploadedAt { get; init; }

    public ReceiptImage(
        Guid id, Guid receiptId, string storageKey, string contentType,
        long fileSizeBytes, string originalFileName, DateTime uploadedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Receipt image id cannot be empty", nameof(id));
        if (receiptId == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(receiptId));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required", nameof(storageKey));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required", nameof(contentType));
        if (fileSizeBytes <= 0)
            throw new ArgumentException("File size must be positive", nameof(fileSizeBytes));
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name is required", nameof(originalFileName));

        Id = id;
        ReceiptId = receiptId;
        StorageKey = storageKey;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        OriginalFileName = originalFileName;
        UploadedAt = uploadedAt;
    }
}
