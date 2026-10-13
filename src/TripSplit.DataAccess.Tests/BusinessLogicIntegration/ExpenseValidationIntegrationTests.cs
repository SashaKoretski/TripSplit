using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;
using TripSplit.DataAccess.Repositories;
using Xunit;

namespace TripSplit.DataAccess.Tests.BusinessLogicIntegration;

// Integration-тест: проверяет, что отказы бизнес-правил ExpenseService (не mock, настоящий
// ExpenseRepository + Postgres) не оставляют частично записанных строк в хранилище.
[Trait("Category", "Integration")]
[Collection("Database")]
public sealed class ExpenseValidationIntegrationTests
{
    private readonly UserService _users;
    private readonly TripService _trips;
    private readonly ExpenseService _expenses;
    private readonly ExpenseRepository _expensesReader;
    private readonly DatabaseFixture _fixture;

    public ExpenseValidationIntegrationTests(DatabaseFixture db)
    {
        var userRepo = new UserRepository(db.Factory);
        var tripRepo = new TripRepository(db.Factory);
        var expenseRepo = new ExpenseRepository(db.Factory);
        var receiptRepo = new ReceiptRepository(db.Factory);

        _users = new UserService(userRepo);
        _trips = new TripService(tripRepo, userRepo);
        _expenses = new ExpenseService(expenseRepo, tripRepo, receiptRepo);
        _expensesReader = new ExpenseRepository(db.Factory);
        _fixture = db;
    }

    [Fact]
    public async Task AddExpense_InvalidParticipantsOrAmounts_ThrowsAndLeavesStorageUnchanged()
    {
        // Arrange - поездка с двумя участниками и один посторонний пользователь
        await _fixture.ResetAsync();
        var sasha = await _users.RegisterAsync("Sasha", "sasha@example.test", "gid-sasha-ev");
        var masha = await _users.RegisterAsync("Masha", "masha@example.test", "gid-masha-ev");
        var dasha = await _users.RegisterAsync("Dasha", "dasha@example.test", "gid-dasha-ev");
        var trip = await _trips.CreateAsync("Day trip", "RUB", sasha.Id);
        await _trips.AddParticipantAsync(trip.Id, masha.Id);

        // Act/Assert 1: плательщик не участник поездки
        await Assert.ThrowsAsync<InvalidExpenseException>(() =>
            _expenses.AddAsync(trip.Id, dasha.Id, "Taxi", ExpenseType.Transport,
                value: 300m, discount: 0m, consumerIds: new[] { sasha.Id }));
        Assert.Empty(await _expensesReader.GetByTripAsync(trip.Id));

        // Act/Assert 2: потребитель не участник поездки
        await Assert.ThrowsAsync<InvalidExpenseException>(() =>
            _expenses.AddAsync(trip.Id, sasha.Id, "Taxi", ExpenseType.Transport,
                value: 300m, discount: 0m, consumerIds: new[] { dasha.Id }));
        Assert.Empty(await _expensesReader.GetByTripAsync(trip.Id));

        // Act/Assert 3: скидка больше суммы траты (проверка на уровне модели Expense,
        // но вызов идет через тот же публичный ExpenseService.AddAsync)
        await Assert.ThrowsAsync<InvalidExpenseException>(() =>
            _expenses.AddAsync(trip.Id, sasha.Id, "Hotel", ExpenseType.Accommodation,
                value: 100m, discount: 150m, consumerIds: new[] { sasha.Id, masha.Id }));
        Assert.Empty(await _expensesReader.GetByTripAsync(trip.Id));

        // Act/Assert 4 (контроль): корректная трата после трех отказов проходит и сохраняется -
        // значит нулевой счет выше не был следствием поломанного соединения или фикстуры
        var valid = await _expenses.AddAsync(trip.Id, sasha.Id, "Lunch", ExpenseType.Food,
            value: 500m, discount: 0m, consumerIds: new[] { sasha.Id, masha.Id });
        var persisted = await _expensesReader.GetByTripAsync(trip.Id);
        Assert.Single(persisted);
        Assert.Equal(valid.Id, persisted[0].Id);
    }
}
