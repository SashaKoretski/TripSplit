using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.Models;

/// <summary>
/// Классический unit-тест доменной модели Transfer.
/// Техника данных: частный случай комбинации (fromUserId == toUserId) и граница amount &lt;= 0.
/// </summary>
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
    public void Ctor_EmptyFromUserId_ThrowsArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Transfer(Guid.Empty, Guid.NewGuid(), 10m));

    [TestMethod]
    public void Ctor_EmptyToUserId_ThrowsArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Transfer(Guid.NewGuid(), Guid.Empty, 10m));

    [TestMethod]
    public void Ctor_SameFromAndToUser_ThrowsArgumentException()
    {
        var id = Guid.NewGuid();
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Transfer(id, id, 10m));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_NonPositiveAmount_ThrowsArgumentException(int amount) =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Transfer(Guid.NewGuid(), Guid.NewGuid(), amount));
}
