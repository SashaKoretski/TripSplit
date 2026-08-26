using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class ExpenseServiceTests
{
    private Mock<IExpenseRepository> _expenses = null!;
    private Mock<ITripRepository> _trips = null!;
    private Mock<IReceiptRepository> _receipts = null!;
    private ExpenseService _sut = null!;

    private readonly Guid _payerId = Guid.NewGuid();
    private readonly Guid _consumer1 = Guid.NewGuid();
    private readonly Guid _consumer2 = Guid.NewGuid();

    private Trip MakeActiveTrip(Guid? id = null)
    {
        var trip = new Trip(id ?? Guid.NewGuid(), "Trip", "RUB", DateTime.UtcNow);
        trip.ParticipantIds.Add(_payerId);
        trip.ParticipantIds.Add(_consumer1);
        trip.ParticipantIds.Add(_consumer2);
        return trip;
    }

    private static Trip MakeFinishedTrip(Trip trip)
    {
        trip.Status = TripStatus.Finished;
        return trip;
    }

    private Expense MakeExpense(Guid tripId, Guid? receiptId = null) =>
        new(Guid.NewGuid(), tripId, _payerId, "Dinner", ExpenseType.Food,
            value: 300m, discount: 0m,
            consumerIds: new[] { _payerId, _consumer1, _consumer2 },
            receiptId: receiptId);

    [TestInitialize]
    public void Setup()
    {
        _expenses = new Mock<IExpenseRepository>();
        _trips = new Mock<ITripRepository>();
        _receipts = new Mock<IReceiptRepository>();
        _sut = new ExpenseService(_expenses.Object, _trips.Object, _receipts.Object);
    }

    [TestMethod]
    public async Task AddAsync_Valid_CreatesAndPersists()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        var expense = await _sut.AddAsync(
            trip.Id, _payerId, "Dinner", ExpenseType.Food, 300m, 30m,
            new[] { _payerId, _consumer1, _consumer2 });

        Assert.IsNotNull(expense);
        Assert.AreNotEqual(Guid.Empty, expense.Id);
        Assert.AreEqual(270m, expense.EffectiveAmount);
        _expenses.Verify(r => r.AddAsync(It.Is<Expense>(e => e.Id == expense.Id)), Times.Once);
    }

    [TestMethod]
    public async Task AddAsync_WithValidReceipt_Persists()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        var receipt = new Receipt(Guid.NewGuid(), trip.Id, "https://s3/x.jpg", new DateOnly(2026, 8, 25));
        _receipts.Setup(r => r.GetByIdAsync(receipt.Id)).ReturnsAsync(receipt);

        var expense = await _sut.AddAsync(
            trip.Id, _payerId, "Dinner", ExpenseType.Food, 300m, 0m,
            new[] { _payerId, _consumer1 }, receipt.Id);

        Assert.AreEqual(receipt.Id, expense.ReceiptId);
        _expenses.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Once);
    }

    [TestMethod]
    public async Task AddAsync_TripNotFound_Throws()
    {
        var tripId = Guid.NewGuid();
        _trips.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsExceptionAsync<TripNotFoundException>(
            () => _sut.AddAsync(tripId, _payerId, "X", ExpenseType.Other, 10m, 0m, new[] { _payerId }));

        _expenses.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
    }

    [TestMethod]
    public async Task AddAsync_TripFinished_Throws()
    {
        var trip = MakeFinishedTrip(MakeActiveTrip());
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<TripAlreadyFinishedException>(
            () => _sut.AddAsync(trip.Id, _payerId, "X", ExpenseType.Other, 10m, 0m, new[] { _payerId }));

        _expenses.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
    }

    [TestMethod]
    public async Task AddAsync_PayerNotParticipant_Throws()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        var strangerId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.AddAsync(trip.Id, strangerId, "X", ExpenseType.Other, 10m, 0m, new[] { _payerId }));

        _expenses.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
    }

    [TestMethod]
    public async Task AddAsync_ConsumerNotParticipant_Throws()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        var strangerId = Guid.NewGuid();

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.AddAsync(trip.Id, _payerId, "X", ExpenseType.Other, 10m, 0m,
                new[] { _payerId, strangerId }));

        _expenses.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
    }

    [TestMethod]
    public async Task AddAsync_ReceiptNotFound_Throws()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        var receiptId = Guid.NewGuid();
        _receipts.Setup(r => r.GetByIdAsync(receiptId)).ReturnsAsync((Receipt?)null);

        await Assert.ThrowsExceptionAsync<ReceiptNotFoundException>(
            () => _sut.AddAsync(trip.Id, _payerId, "X", ExpenseType.Other, 10m, 0m,
                new[] { _payerId }, receiptId));

        _expenses.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
    }

    [TestMethod]
    public async Task AddAsync_ReceiptFromAnotherTrip_Throws()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        var otherReceipt = new Receipt(Guid.NewGuid(), Guid.NewGuid(), "https://s3/y.jpg", new DateOnly(2026, 8, 25));
        _receipts.Setup(r => r.GetByIdAsync(otherReceipt.Id)).ReturnsAsync(otherReceipt);

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.AddAsync(trip.Id, _payerId, "X", ExpenseType.Other, 10m, 0m,
                new[] { _payerId }, otherReceipt.Id));

        _expenses.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
    }

    [TestMethod]
    public async Task AddAsync_EmptyTripId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.AddAsync(Guid.Empty, _payerId, "X", ExpenseType.Other, 10m, 0m, new[] { _payerId }));

        _trips.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task AddAsync_NullConsumers_ThrowsInvalidExpense()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.AddAsync(trip.Id, _payerId, "X", ExpenseType.Other, 10m, 0m, null!));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task AddAsync_NonPositiveValue_Throws(int value)
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.AddAsync(trip.Id, _payerId, "X", ExpenseType.Other, value, 0m, new[] { _payerId }));
    }

    [TestMethod]
    public async Task AddAsync_DiscountExceedsValue_Throws()
    {
        var trip = MakeActiveTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.AddAsync(trip.Id, _payerId, "X", ExpenseType.Other, 100m, 200m, new[] { _payerId }));
    }

    [TestMethod]
    public async Task GetByIdAsync_Existing_Returns()
    {
        var expense = MakeExpense(Guid.NewGuid());
        _expenses.Setup(r => r.GetByIdAsync(expense.Id)).ReturnsAsync(expense);

        var result = await _sut.GetByIdAsync(expense.Id);

        Assert.AreSame(expense, result);
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        var id = Guid.NewGuid();
        _expenses.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Expense?)null);

        await Assert.ThrowsExceptionAsync<ExpenseNotFoundException>(
            () => _sut.GetByIdAsync(id));
    }

    [TestMethod]
    public async Task GetByIdAsync_EmptyId_Throws() =>
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.GetByIdAsync(Guid.Empty));

    [TestMethod]
    public async Task GetByTripAsync_Returns()
    {
        var tripId = Guid.NewGuid();
        _expenses.Setup(r => r.GetByTripAsync(tripId))
                 .ReturnsAsync(new List<Expense> { MakeExpense(tripId), MakeExpense(tripId) });

        var result = await _sut.GetByTripAsync(tripId);

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetByTripAsync_EmptyId_Throws() =>
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.GetByTripAsync(Guid.Empty));

    [TestMethod]
    public async Task UpdateAsync_Valid_Persists()
    {
        var trip = MakeActiveTrip();
        var existing = MakeExpense(trip.Id);
        var updated = new Expense(existing.Id, trip.Id, _payerId, "Updated",
            ExpenseType.Food, 500m, 50m, new[] { _payerId, _consumer1 });

        _expenses.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await _sut.UpdateAsync(updated);

        _expenses.Verify(r => r.UpdateAsync(updated), Times.Once);
    }

    [TestMethod]
    public async Task UpdateAsync_Null_Throws() =>
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => _sut.UpdateAsync(null!));

    [TestMethod]
    public async Task UpdateAsync_ExpenseNotFound_Throws()
    {
        var expense = MakeExpense(Guid.NewGuid());
        _expenses.Setup(r => r.GetByIdAsync(expense.Id)).ReturnsAsync((Expense?)null);

        await Assert.ThrowsExceptionAsync<ExpenseNotFoundException>(
            () => _sut.UpdateAsync(expense));
    }

    [TestMethod]
    public async Task UpdateAsync_MovingToAnotherTrip_Throws()
    {
        var originalTripId = Guid.NewGuid();
        var otherTripId = Guid.NewGuid();
        var existing = MakeExpense(originalTripId);
        var updated = new Expense(existing.Id, otherTripId, _payerId, "X",
            ExpenseType.Other, 10m, 0m, new[] { _payerId });

        _expenses.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.UpdateAsync(updated));
    }

    [TestMethod]
    public async Task UpdateAsync_TripFinished_Throws()
    {
        var trip = MakeFinishedTrip(MakeActiveTrip());
        var existing = MakeExpense(trip.Id);
        _expenses.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<TripAlreadyFinishedException>(
            () => _sut.UpdateAsync(existing));
    }

    [TestMethod]
    public async Task UpdateAsync_ConsumerNotParticipant_Throws()
    {
        var trip = MakeActiveTrip();
        var existing = MakeExpense(trip.Id);
        var strangerId = Guid.NewGuid();
        var updated = new Expense(existing.Id, trip.Id, _payerId, "X",
            ExpenseType.Other, 10m, 0m, new[] { _payerId, strangerId });

        _expenses.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<InvalidExpenseException>(
            () => _sut.UpdateAsync(updated));
    }

    [TestMethod]
    public async Task DeleteAsync_Existing_Deletes()
    {
        var trip = MakeActiveTrip();
        var expense = MakeExpense(trip.Id);
        _expenses.Setup(r => r.GetByIdAsync(expense.Id)).ReturnsAsync(expense);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await _sut.DeleteAsync(expense.Id);

        _expenses.Verify(r => r.DeleteAsync(expense.Id), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAsync_NotFound_Throws()
    {
        var id = Guid.NewGuid();
        _expenses.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Expense?)null);

        await Assert.ThrowsExceptionAsync<ExpenseNotFoundException>(
            () => _sut.DeleteAsync(id));

        _expenses.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteAsync_TripFinished_Throws()
    {
        var trip = MakeFinishedTrip(MakeActiveTrip());
        var expense = MakeExpense(trip.Id);
        _expenses.Setup(r => r.GetByIdAsync(expense.Id)).ReturnsAsync(expense);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<TripAlreadyFinishedException>(
            () => _sut.DeleteAsync(expense.Id));

        _expenses.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteAsync_EmptyId_Throws() =>
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => _sut.DeleteAsync(Guid.Empty));
}
