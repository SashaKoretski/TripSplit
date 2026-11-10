using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests.Models;

[TestClass]
public class ReceiptTests
{
    [TestMethod]
    public void Ctor_ValidArguments_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 25);

        var receipt = new Receipt(id, tripId, "Кафе", date);

        Assert.AreEqual(id, receipt.Id);
        Assert.AreEqual(tripId, receipt.TripId);
        Assert.AreEqual("Кафе", receipt.Name);
        Assert.AreEqual(date, receipt.Date);
    }

    [TestMethod]
    public void Ctor_EmptyId_ThrowsArgumentException()
    {
        var act = () => { _ = new Receipt(Guid.Empty, Guid.NewGuid(), "X", default); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void Ctor_EmptyTripId_ThrowsArgumentException()
    {
        var act = () => { _ = new Receipt(Guid.NewGuid(), Guid.Empty, "X", default); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankName_ThrowsArgumentException(string name)
    {
        var act = () => { _ = new Receipt(Guid.NewGuid(), Guid.NewGuid(), name, default); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void ReceiptBuilder_WithOverrides_BuildsExpectedReceipt()
    {
        var tripId = Guid.NewGuid();
        var receipt = new ReceiptBuilder().ForTrip(tripId).WithName("Такси").Build();

        Assert.AreEqual(tripId, receipt.TripId);
        Assert.AreEqual("Такси", receipt.Name);
    }
}
