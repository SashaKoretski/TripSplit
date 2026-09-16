namespace TripSplit.BusinessLogic.Configuration;

// Ограничения на загружаемые изображения чеков
public sealed class ReceiptImageOptions
{
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    public IReadOnlySet<string> AllowedContentTypes { get; init; } = new HashSet<string>
    {
        "image/jpeg", "image/png", "image/webp", "application/pdf"
    };
}
