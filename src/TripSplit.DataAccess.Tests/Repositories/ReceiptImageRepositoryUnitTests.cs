using Moq;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Repositories;

public sealed class ReceiptImageRepositoryUnitTests
{
    private static ReceiptImage NewImage(Guid? receiptId = null) => new(
        Guid.NewGuid(), receiptId ?? Guid.NewGuid(), "receipts/t/r/x.jpg",
        "image/jpeg", 1024, "check.jpg", DateTime.UtcNow);

    private static Dictionary<string, object?> RowOf(ReceiptImage i) => new()
    {
        ["id"] = i.Id,
        ["receipt_id"] = i.ReceiptId,
        ["storage_key"] = i.StorageKey,
        ["content_type"] = i.ContentType,
        ["file_size_bytes"] = i.FileSizeBytes,
        ["original_file_name"] = i.OriginalFileName,
        ["uploaded_at"] = i.UploadedAt,
    };

    [Fact]
    public async Task GetByReceiptAsync_RowExists_ReturnsMappedImage()
    {
        var image = NewImage();
        var conn = new FakeDbConnection(FakeCommandScript.Reader(RowOf(image)));
        var repo = new ReceiptImageRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByReceiptAsync(image.ReceiptId);

        Assert.Equal(image.StorageKey, result!.StorageKey);
    }

    [Fact]
    public async Task GetByReceiptAsync_EmptyReceiptId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ReceiptImageRepository(factory.Object);

        var act = () => repo.GetByReceiptAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByReceiptAsync_NoMatchingRow_ReturnsNull()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new ReceiptImageRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByReceiptAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_Valid_ExecutesInsert()
    {
        var image = NewImage();
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new ReceiptImageRepository(new FakeDbConnectionFactory(conn));

        await repo.AddAsync(image);

        Assert.Contains("INSERT INTO receipt_images", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task AddAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ReceiptImageRepository(factory.Object);

        var act = () => repo.AddAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DeleteAsync_RowAffected_CompletesSuccessfully()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new ReceiptImageRepository(new FakeDbConnectionFactory(conn));

        await repo.DeleteAsync(Guid.NewGuid());

        Assert.Contains("DELETE FROM receipt_images", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task DeleteAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new ReceiptImageRepository(new FakeDbConnectionFactory(conn));

        var act = () => repo.DeleteAsync(Guid.NewGuid());

        await Assert.ThrowsAsync<EntityNotFoundException>(act);
    }

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ReceiptImageRepository(factory.Object);

        var act = () => repo.DeleteAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }
}
