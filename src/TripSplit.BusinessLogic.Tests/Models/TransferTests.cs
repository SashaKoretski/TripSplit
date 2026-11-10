using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.Models;

[TestClass]
public class TransferTests
{
    [TestMethod]
    public void Ctor_ValidArguments_SetsAllProperties()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var transfer = new Transfer(from, to, 50m);

        Assert.AreEqual(from, transfer.FromUserId);
        Assert.AreEqual(to, transfer.ToUserId);
        Assert.AreEqual(50m, transfer.Amount);
    }

    [TestMethod]
    public void Ctor_EmptyFromUserId_ThrowsArgumentException()
    {
        var act = () => { _ = new Transfer(Guid.Empty, Guid.NewGuid(), 10m); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void Ctor_EmptyToUserId_ThrowsArgumentException()
    {
        var act = () => { _ = new Transfer(Guid.NewGuid(), Guid.Empty, 10m); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void Ctor_SameFromAndToUser_ThrowsArgumentException()
    {
        var id = Guid.NewGuid();

        var act = () => { _ = new Transfer(id, id, 10m); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_NonPositiveAmount_ThrowsArgumentException(int amount)
    {
        var act = () => { _ = new Transfer(Guid.NewGuid(), Guid.NewGuid(), amount); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }
}
