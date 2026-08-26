using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class DebtSettlementServiceTests
{
    private Mock<ITripRepository> _trips = null!;
    private Mock<IExpenseRepository> _expenses = null!;
    private Mock<IDebtMinimizationStrategy> _strategy = null!;
    private DebtSettlementService _sut = null!;

    private static Trip MakeTrip(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Test", "RUB", DateTime.UtcNow);

    private static Expense MakeExpense(Guid tripId, Guid payerId, decimal value,
        IEnumerable<Guid> consumerIds, decimal discount = 0m) =>
        new(Guid.NewGuid(), tripId, payerId, "X", ExpenseType.Other,
            value, discount, consumerIds);

    [TestInitialize]
    public void Setup()
    {
        _trips = new Mock<ITripRepository>();
        _expenses = new Mock<IExpenseRepository>();
        _strategy = new Mock<IDebtMinimizationStrategy>();

        _strategy.Setup(s => s.Minimize(It.IsAny<IReadOnlyDictionary<Guid, decimal>>()))
                 .Returns(Array.Empty<Transfer>());

        _sut = new DebtSettlementService(_trips.Object, _expenses.Object, _strategy.Object);
    }

    [TestMethod]
    public async Task Calculate_EmptyTripId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.CalculateSettlementAsync(Guid.Empty));

        _trips.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task Calculate_TripNotFound_Throws()
    {
        var tripId = Guid.NewGuid();
        _trips.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsExceptionAsync<TripNotFoundException>(
            () => _sut.CalculateSettlementAsync(tripId));

        _expenses.Verify(r => r.GetByTripAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task Calculate_NoExpenses_ReturnsEmptyStatistics()
    {
        var trip = MakeTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _expenses.Setup(r => r.GetByTripAsync(trip.Id)).ReturnsAsync(new List<Expense>());

        var stats = await _sut.CalculateSettlementAsync(trip.Id);

        Assert.AreEqual(trip.Id, stats.TripId);
        Assert.AreEqual(0m, stats.TotalSpent);
        Assert.AreEqual(0, stats.PerUserSpent.Count);
        Assert.AreEqual(0, stats.Transfers.Count);
        _strategy.Verify(s => s.Minimize(It.IsAny<IReadOnlyDictionary<Guid, decimal>>()), Times.Never);
    }

    [TestMethod]
    public async Task Calculate_TotalSpent_IncludesDiscounts()
    {
        var trip = MakeTrip();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var expenses = new List<Expense>
        {
            MakeExpense(trip.Id, a, value: 100m, consumerIds: new[] { a, b }, discount: 20m),
            MakeExpense(trip.Id, b, value: 50m,  consumerIds: new[] { a, b }),
        };
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _expenses.Setup(r => r.GetByTripAsync(trip.Id)).ReturnsAsync(expenses);

        var stats = await _sut.CalculateSettlementAsync(trip.Id);

        Assert.AreEqual(130m, stats.TotalSpent);
    }

    [TestMethod]
    public async Task Calculate_PerUserSpent_GroupedByPayer()
    {
        var trip = MakeTrip();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var expenses = new List<Expense>
        {
            MakeExpense(trip.Id, a, 100m, new[] { a, b }),
            MakeExpense(trip.Id, a, 40m,  new[] { a, b }),
            MakeExpense(trip.Id, b, 60m,  new[] { a, b }),
        };
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _expenses.Setup(r => r.GetByTripAsync(trip.Id)).ReturnsAsync(expenses);

        var stats = await _sut.CalculateSettlementAsync(trip.Id);

        Assert.AreEqual(140m, stats.PerUserSpent[a]);
        Assert.AreEqual(60m, stats.PerUserSpent[b]);
    }

    [TestMethod]
    public async Task Calculate_SingleExpense_ComputesCorrectBalances()
    {
        // A заплатил 90, потребители: A, B, C — каждому доля 30. Балансы: A=+60, B=−30, C=−30.
        var trip = MakeTrip();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var expenses = new List<Expense>
        {
            MakeExpense(trip.Id, a, 90m, new[] { a, b, c }),
        };
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _expenses.Setup(r => r.GetByTripAsync(trip.Id)).ReturnsAsync(expenses);

        IReadOnlyDictionary<Guid, decimal>? captured = null;
        _strategy.Setup(s => s.Minimize(It.IsAny<IReadOnlyDictionary<Guid, decimal>>()))
                 .Callback<IReadOnlyDictionary<Guid, decimal>>(d => captured = d)
                 .Returns(Array.Empty<Transfer>());

        await _sut.CalculateSettlementAsync(trip.Id);

        Assert.IsNotNull(captured);
        Assert.AreEqual(60m, captured![a]);
        Assert.AreEqual(-30m, captured[b]);
        Assert.AreEqual(-30m, captured[c]);
    }

    [TestMethod]
    public async Task Calculate_MultipleExpenses_BalancesAccumulate()
    {
        // A потратил 60 на A,B (доля 30). B потратил 40 на A,B (доля 20)
        // Балансы: A = +60 - 30 - 20 = +10. B = +40 - 30 - 20 = -10
        var trip = MakeTrip();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var expenses = new List<Expense>
        {
            MakeExpense(trip.Id, a, 60m, new[] { a, b }),
            MakeExpense(trip.Id, b, 40m, new[] { a, b }),
        };
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _expenses.Setup(r => r.GetByTripAsync(trip.Id)).ReturnsAsync(expenses);

        IReadOnlyDictionary<Guid, decimal>? captured = null;
        _strategy.Setup(s => s.Minimize(It.IsAny<IReadOnlyDictionary<Guid, decimal>>()))
                 .Callback<IReadOnlyDictionary<Guid, decimal>>(d => captured = d)
                 .Returns(Array.Empty<Transfer>());

        await _sut.CalculateSettlementAsync(trip.Id);

        Assert.AreEqual(10m, captured![a]);
        Assert.AreEqual(-10m, captured[b]);
    }

    [TestMethod]
    public async Task Calculate_ReturnsTransfersFromStrategy()
    {
        var trip = MakeTrip();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var expenses = new List<Expense>
        {
            MakeExpense(trip.Id, a, 100m, new[] { a, b }),
        };
        var expectedTransfers = new List<Transfer> { new(b, a, 50m) };

        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _expenses.Setup(r => r.GetByTripAsync(trip.Id)).ReturnsAsync(expenses);
        _strategy.Setup(s => s.Minimize(It.IsAny<IReadOnlyDictionary<Guid, decimal>>()))
                 .Returns(expectedTransfers);

        var stats = await _sut.CalculateSettlementAsync(trip.Id);

        Assert.AreEqual(1, stats.Transfers.Count);
        Assert.AreSame(expectedTransfers[0], stats.Transfers[0]);
        _strategy.Verify(s => s.Minimize(It.IsAny<IReadOnlyDictionary<Guid, decimal>>()), Times.Once);
    }
}
