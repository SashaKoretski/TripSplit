using Moq;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Repositories;

public sealed class ReceiptRepositoryUnitTests
{
    private static Receipt NewReceipt(Guid? tripId = null) =>
        new(Guid.NewGuid(), tripId ?? Guid.NewGuid(), "Кафе", new DateOnly(2026, 8, 25));

    private static Dictionary<string, object?> RowOf(Receipt r) => new()
    {
        ["id"] = r.Id,
        ["trip_id"] = r.TripId,
        ["name"] = r.Name,
        ["date"] = r.Date,
    };

    [Fact]
    public async Task GetByIdAsync_RowExists_ReturnsMappedReceipt()
    {
        var receipt = NewReceipt();
        var conn = new FakeDbConnection(FakeCommandScript.Reader(RowOf(receipt)));
        var repo = new ReceiptRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(receipt.Id);

        Assert.Equal(receipt.Name, result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ReceiptRepository(factory.Object);

        var act = () => repo.GetByIdAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatchingRow_ReturnsNull()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new ReceiptRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTripAsync_NoReceipts_ReturnsEmptyList()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new ReceiptRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByTripAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTripAsync_EmptyTripId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ReceiptRepository(factory.Object);

        var act = () => repo.GetByTripAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task AddAsync_Valid_ExecutesInsert()
    {
        var receipt = NewReceipt();
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new ReceiptRepository(new FakeDbConnectionFactory(conn));

        await repo.AddAsync(receipt);

        Assert.Contains("INSERT INTO receipts", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task AddAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ReceiptRepository(factory.Object);

        var act = () => repo.AddAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DeleteAsync_RowAffected_CompletesSuccessfully()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new ReceiptRepository(new FakeDbConnectionFactory(conn));

        await repo.DeleteAsync(Guid.NewGuid());

        Assert.Contains("DELETE FROM receipts", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task DeleteAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new ReceiptRepository(new FakeDbConnectionFactory(conn));

        var act = () => repo.DeleteAsync(Guid.NewGuid());

        await Assert.ThrowsAsync<EntityNotFoundException>(act);
    }

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ReceiptRepository(factory.Object);

        var act = () => repo.DeleteAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }
}
