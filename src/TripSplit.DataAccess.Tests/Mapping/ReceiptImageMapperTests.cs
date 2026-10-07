using TripSplit.DataAccess.Mapping;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Mapping;

/// <summary>
/// Unit-тест маппинга строки receipt_images (без реальной БД).
/// Техника данных: граничное значение размера файла (1 байт как минимально допустимое ненулевое значение).
/// </summary>
public sealed class ReceiptImageMapperTests
{
    private static Dictionary<string, object?> Row(Guid id, Guid receiptId, long sizeBytes) => new()
    {
        ["id"] = id,
        ["receipt_id"] = receiptId,
        ["storage_key"] = "receipts/t/r/x.jpg",
        ["content_type"] = "image/jpeg",
        ["file_size_bytes"] = sizeBytes,
        ["original_file_name"] = "check.jpg",
        ["uploaded_at"] = new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void Map_ValidRow_ReturnsReceiptImageWithAllFields()
    {
        var id = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var reader = new FakeDbDataReader(new[] { Row(id, receiptId, 2048) });
        reader.Read();

        var image = ReceiptImageMapper.Map(reader);

        Assert.Equal(id, image.Id);
        Assert.Equal(receiptId, image.ReceiptId);
        Assert.Equal("receipts/t/r/x.jpg", image.StorageKey);
        Assert.Equal("image/jpeg", image.ContentType);
        Assert.Equal(2048, image.FileSizeBytes);
        Assert.Equal("check.jpg", image.OriginalFileName);
    }

    [Fact]
    public void Map_MinimalNonZeroFileSize_IsPreservedExactly()
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), Guid.NewGuid(), 1) });
        reader.Read();

        var image = ReceiptImageMapper.Map(reader);

        Assert.Equal(1, image.FileSizeBytes);
    }
}
