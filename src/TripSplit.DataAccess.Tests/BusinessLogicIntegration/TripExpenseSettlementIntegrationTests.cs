using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;
using TripSplit.DataAccess.Repositories;
using Xunit;

namespace TripSplit.DataAccess.Tests.BusinessLogicIntegration;

// Integration-тест: бизнес-логика (TripService/ExpenseService/DebtSettlementService/UserService)
// поверх настоящих репозиториев и настоящего Postgres (см. DatabaseFixture) - без единого mock.
// Один файл - один сквозной сценарий из нескольких последовательных Act/Assert (ТИОПО ЛР2, Т17.a/b).
[Trait("Category", "Integration")]
[Collection("Database")]
public sealed class TripExpenseSettlementIntegrationTests
{
    private readonly UserService _users;
    private readonly TripService _trips;
    private readonly ExpenseService _expenses;
    private readonly DebtSettlementService _settlement;

    // отдельные экземпляры репозиториев для перечитывания состояния из БД "со стороны",
    // а не через тот же объект, которым писал сервис - так тест доказывает реальную persistence
    private readonly TripRepository _tripsReader;
    private readonly ExpenseRepository _expensesReader;

    public TripExpenseSettlementIntegrationTests(DatabaseFixture db)
    {
        var userRepo = new UserRepository(db.Factory);
        var tripRepo = new TripRepository(db.Factory);
        var expenseRepo = new ExpenseRepository(db.Factory);
        var receiptRepo = new ReceiptRepository(db.Factory);

        _users = new UserService(userRepo);
        _trips = new TripService(tripRepo, userRepo);
        _expenses = new ExpenseService(expenseRepo, tripRepo, receiptRepo);
        _settlement = new DebtSettlementService(
            tripRepo, expenseRepo, new GreedyDebtMinimizationStrategy(new DebtSettlementOptions()));

        _tripsReader = new TripRepository(db.Factory);
        _expensesReader = new ExpenseRepository(db.Factory);

        _fixture = db;
    }

    private readonly DatabaseFixture _fixture;

    [Fact]
    public async Task CreateTrip_AddExpense_Finish_PersistsConsistentStateAcrossRealDatabase()
    {
        // Arrange - один раз, дальше только последовательные Act/Assert шаги сценария
        await _fixture.ResetAsync();
        var sasha = await _users.RegisterAsync("Sasha", "sasha@example.test", "gid-sasha-ts");
        var masha = await _users.RegisterAsync("Masha", "masha@example.test", "gid-masha-ts");

        // Act 1: создание поездки
        var trip = await _trips.CreateAsync("Weekend in Sochi", "rub", sasha.Id);

        // Assert 1: поездка реально записана в БД (перечитана новым репозиторием), а не только
        // возвращена в памяти; валюта нормализована к верхнему регистру, организатор - участник
        var persistedAfterCreate = await _tripsReader.GetByIdAsync(trip.Id);
        Assert.NotNull(persistedAfterCreate);
        Assert.Equal("RUB", persistedAfterCreate!.Currency);
        Assert.Equal(TripStatus.Active, persistedAfterCreate.Status);
        Assert.Equal(new[] { sasha.Id }, persistedAfterCreate.ParticipantIds);

        // Act 2: приглашение второго участника
        await _trips.AddParticipantAsync(trip.Id, masha.Id);

        // Assert 2: оба участника видны при перечитывании из БД
        var persistedAfterInvite = await _tripsReader.GetByIdAsync(trip.Id);
        Assert.Equal(
            new[] { sasha.Id, masha.Id }.OrderBy(g => g),
            persistedAfterInvite!.ParticipantIds.OrderBy(g => g));

        // Act 3: добавление траты (Саша платит за ужин, делят с Машей)
        var expense = await _expenses.AddAsync(
            trip.Id, sasha.Id, "Dinner", ExpenseType.Food,
            value: 1000m, discount: 200m, consumerIds: new[] { sasha.Id, masha.Id });

        // Assert 3: трата записана с корректной эффективной суммой (value - discount)
        var persistedExpense = await _expensesReader.GetByIdAsync(expense.Id);
        Assert.NotNull(persistedExpense);
        Assert.Equal(800m, persistedExpense!.EffectiveAmount);
        Assert.Equal(
            new[] { sasha.Id, masha.Id }.OrderBy(g => g),
            persistedExpense.ConsumerIds.OrderBy(g => g));

        // Act 4: расчет долгов по реально сохраненным тратам
        var stats = await _settlement.CalculateSettlementAsync(trip.Id);

        // Assert 4: Маша должна Саше половину эффективной суммы (400 из 800)
        Assert.Equal(800m, stats.TotalSpent);
        Assert.Equal(800m, stats.PerUserSpent[sasha.Id]);
        var transfer = Assert.Single(stats.Transfers);
        Assert.Equal(masha.Id, transfer.FromUserId);
        Assert.Equal(sasha.Id, transfer.ToUserId);
        Assert.Equal(400m, transfer.Amount);

        // Act 5: завершение поездки
        await _trips.FinishAsync(trip.Id);

        // Assert 5: статус завершения реально сохранен
        var finished = await _tripsReader.GetByIdAsync(trip.Id);
        Assert.Equal(TripStatus.Finished, finished!.Status);

        // Act 6 (авария): попытка добавить трату в завершенную поездку
        await Assert.ThrowsAsync<TripAlreadyFinishedException>(() =>
            _expenses.AddAsync(trip.Id, sasha.Id, "Late snack", ExpenseType.Food,
                value: 50m, discount: 0m, consumerIds: new[] { sasha.Id }));

        // Assert 6: отказ бизнес-логики не оставил частично записанных данных в хранилище
        var expensesAfterRejectedAdd = await _expensesReader.GetByTripAsync(trip.Id);
        Assert.Single(expensesAfterRejectedAdd);
    }
}
