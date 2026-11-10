using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests.Models;

[TestClass]
public class ExpenseTests
{
    private static Guid[] OneConsumer() => new[] { Guid.NewGuid() };

    [TestMethod]
    public void Ctor_ValidArguments_SetsAllPropertiesAndComputesEffectiveAmount()
    {
        var id = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var payerId = Guid.NewGuid();
        var consumers = new[] { payerId, Guid.NewGuid() };

        var expense = new Expense(id, tripId, payerId, "Dinner", ExpenseType.Food, 300m, 30m, consumers);

        Assert.AreEqual(id, expense.Id);
        Assert.AreEqual(tripId, expense.TripId);
        Assert.AreEqual(270m, expense.EffectiveAmount);
        Assert.IsNull(expense.ReceiptId);
    }

    [TestMethod]
    public void Ctor_EmptyId_ThrowsArgumentException()
    {
        var act = () => { _ = new Expense(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 10m, 0m, OneConsumer()); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void Ctor_EmptyTripId_ThrowsArgumentException()
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "X", ExpenseType.Other, 10m, 0m, OneConsumer()); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void Ctor_EmptyPayerId_ThrowsArgumentException()
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "X", ExpenseType.Other, 10m, 0m, OneConsumer()); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankName_ThrowsInvalidExpenseException(string name)
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), name, ExpenseType.Other, 10m, 0m, OneConsumer()); };

        Assert.ThrowsExactly<InvalidExpenseException>(act);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_NonPositiveValue_ThrowsInvalidExpenseException(int value)
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, value, 0m, OneConsumer()); };

        Assert.ThrowsExactly<InvalidExpenseException>(act);
    }

    [TestMethod]
    public void Ctor_SmallestPositiveValue_IsAccepted()
    {
        var expense = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 0.01m, 0m, OneConsumer());

        Assert.AreEqual(0.01m, expense.Value);
    }

    [TestMethod]
    public void Ctor_NegativeDiscount_ThrowsInvalidExpenseException()
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 10m, -1m, OneConsumer()); };

        Assert.ThrowsExactly<InvalidExpenseException>(act);
    }

    [TestMethod]
    public void Ctor_DiscountEqualsValue_IsAcceptedWithZeroEffectiveAmount()
    {
        var expense = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 100m, 100m, OneConsumer());

        Assert.AreEqual(0m, expense.EffectiveAmount);
    }

    [TestMethod]
    public void Ctor_DiscountExceedsValueByOneCent_ThrowsInvalidExpenseException()
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 100m, 100.01m, OneConsumer()); };

        Assert.ThrowsExactly<InvalidExpenseException>(act);
    }

    [TestMethod]
    public void Ctor_NullConsumers_ThrowsInvalidExpenseException()
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 10m, 0m, null!); };

        Assert.ThrowsExactly<InvalidExpenseException>(act);
    }

    [TestMethod]
    public void Ctor_EmptyConsumersList_ThrowsInvalidExpenseException()
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 10m, 0m, Array.Empty<Guid>()); };

        Assert.ThrowsExactly<InvalidExpenseException>(act);
    }

    [TestMethod]
    public void Ctor_ConsumerIdIsEmptyGuid_ThrowsInvalidExpenseException()
    {
        var act = () => { _ = new Expense(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "X", ExpenseType.Other, 10m, 0m, new[] { Guid.Empty }); };

        Assert.ThrowsExactly<InvalidExpenseException>(act);
    }

    [TestMethod]
    public void ExpenseBuilder_WithOverrides_BuildsExpectedExpense()
    {
        var tripId = Guid.NewGuid();
        var expense = new ExpenseBuilder().ForTrip(tripId).WithValue(500m).WithDiscount(50m).Build();

        Assert.AreEqual(tripId, expense.TripId);
        Assert.AreEqual(450m, expense.EffectiveAmount);
    }
}
