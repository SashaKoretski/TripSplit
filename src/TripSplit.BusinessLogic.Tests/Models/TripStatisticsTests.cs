using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.Models;

/// <summary>Классический unit-тест доменной модели TripStatistics.</summary>
[TestClass]
public class TripStatisticsTests
{
    [TestMethod]
    public void Ctor_ValidArguments_SetsAllProperties()
    {
        var tripId = Guid.NewGuid();
        var perUser = new Dictionary<Guid, decimal> { [Guid.NewGuid()] = 100m };
        var transfers = new List<Transfer> { new(Guid.NewGuid(), Guid.NewGuid(), 50m) };

        var stats = new TripStatistics(tripId, 100m, perUser, transfers);

        Assert.AreEqual(tripId, stats.TripId);
        Assert.AreEqual(100m, stats.TotalSpent);
        Assert.AreSame(perUser, stats.PerUserSpent);
        Assert.AreSame(transfers, stats.Transfers);
    }

    [TestMethod]
    public void Ctor_EmptyTripId_ThrowsArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new TripStatistics(Guid.Empty, 0m, new Dictionary<Guid, decimal>(), Array.Empty<Transfer>()));

    [TestMethod]
    public void Ctor_NullPerUserSpent_ThrowsArgumentNullException() =>
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new TripStatistics(Guid.NewGuid(), 0m, null!, Array.Empty<Transfer>()));

    [TestMethod]
    public void Ctor_NullTransfers_ThrowsArgumentNullException() =>
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new TripStatistics(Guid.NewGuid(), 0m, new Dictionary<Guid, decimal>(), null!));
}
