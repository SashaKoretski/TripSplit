using Moq;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Repositories;

/// <summary>
/// Unit-тесты ExpenseRepository на поддельном ADO.NET (без реальной БД).
/// GetByIdAsync/GetByTripAsync выполняют 2 запроса (трата(ы) + потребители) — очередь
/// сценариев FakeDbConnection настроена в этом порядке.
/// Техника данных: классы эквивалентности (найдено/не найдено), пустая коллекция как граница.
/// </summary>
public sealed class ExpenseRepositoryUnitTests
{
    private static Expense NewExpense(Guid? tripId = null) => new(
        Guid.NewGuid(), tripId ?? Guid.NewGuid(), Guid.NewGuid(), "Dinner",
        ExpenseType.Food, 300m, 30m, new[] { Guid.NewGuid() });

    private static Dictionary<string, object?> RowOf(Expense e) => new()
    {
        ["id"] = e.Id,
        ["trip_id"] = e.TripId,
        ["receipt_id"] = e.ReceiptId,
        ["payer_id"] = e.PayerId,
        ["name"] = e.Name,
        ["type"] = "food",
        ["value"] = e.Value,
        ["discount"] = e.Discount,
    };

    [Fact]
    public async Task GetByIdAsync_RowExists_ReturnsMappedExpenseWithConsumers()
    {
        var expense = NewExpense();
        var consumerId = Guid.NewGuid();
        var conn = new FakeDbConnection(
            FakeCommandScript.Reader(RowOf(expense)),
            FakeCommandScript.Reader(new Dictionary<string, object?> { ["expense_id"] = expense.Id, ["user_id"] = consumerId }));
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(expense.Id);

        Assert.Equal(expense.Id, result!.Id);
        Assert.Contains(consumerId, result.ConsumerIds);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ExpenseRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatchingRow_ReturnsNull()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTripAsync_NoExpenses_ReturnsEmptyListWithoutLoadingConsumers()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByTripAsync(Guid.NewGuid());

        Assert.Empty(result);
        Assert.Single(conn.ExecutedCommands);
    }

    [Fact]
    public async Task GetByTripAsync_EmptyTripId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ExpenseRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetByTripAsync(Guid.Empty));
    }

    [Fact]
    public async Task AddAsync_Valid_InsertsExpenseAndConsumers()
    {
        var expense = NewExpense();
        var conn = new FakeDbConnection(
            FakeCommandScript.NonQuery(1), // insert expense
            FakeCommandScript.NonQuery(0), // delete old consumers
            FakeCommandScript.NonQuery(1)); // insert consumer
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        await repo.AddAsync(expense);

        Assert.Contains("INSERT INTO expenses", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task AddAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ExpenseRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var expense = NewExpense();
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        await Assert.ThrowsAsync<EntityNotFoundException>(() => repo.UpdateAsync(expense));
    }

    [Fact]
    public async Task UpdateAsync_RowAffected_ReplacesConsumers()
    {
        var expense = NewExpense();
        var conn = new FakeDbConnection(
            FakeCommandScript.NonQuery(1),
            FakeCommandScript.NonQuery(0),
            FakeCommandScript.NonQuery(1));
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        await repo.UpdateAsync(expense);

        Assert.Contains("UPDATE expenses", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task UpdateAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ExpenseRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_RowAffected_CompletesSuccessfully()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        await repo.DeleteAsync(Guid.NewGuid());

        Assert.Contains("DELETE FROM expenses", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task DeleteAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new ExpenseRepository(new FakeDbConnectionFactory(conn));

        await Assert.ThrowsAsync<EntityNotFoundException>(() => repo.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new ExpenseRepository(factory.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.DeleteAsync(Guid.Empty));
    }
}
