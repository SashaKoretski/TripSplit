using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests.Models;

[TestClass]
public class TripTests
{
    [TestMethod]
    public void Ctor_ValidArguments_CreatesActiveTripWithUppercaseCurrency()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var trip = new Trip(id, "Bali 2026", "usd", createdAt);

        Assert.AreEqual(id, trip.Id);
        Assert.AreEqual("Bali 2026", trip.Name);
        Assert.AreEqual("USD", trip.Currency);
        Assert.AreEqual(TripStatus.Active, trip.Status);
        Assert.AreEqual(createdAt, trip.CreatedAt);
        Assert.AreEqual(0, trip.ParticipantIds.Count);
    }

    [TestMethod]
    public void Ctor_EmptyId_ThrowsArgumentException()
    {
        var act = () => { _ = new Trip(Guid.Empty, "Trip", "RUB", DateTime.UtcNow); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankName_ThrowsArgumentException(string name)
    {
        var act = () => { _ = new Trip(Guid.NewGuid(), name, "RUB", DateTime.UtcNow); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    [DataRow("RU")]
    [DataRow("RUBL")]
    [DataRow("")]
    public void Ctor_CurrencyLengthNot3_ThrowsArgumentException(string currency)
    {
        var act = () => { _ = new Trip(Guid.NewGuid(), "Trip", currency, DateTime.UtcNow); };

        Assert.ThrowsExactly<ArgumentException>(act);
    }

    [TestMethod]
    public void Ctor_CurrencyExactly3Letters_IsAccepted()
    {
        var trip = new Trip(Guid.NewGuid(), "Trip", "eur", DateTime.UtcNow);

        Assert.AreEqual("EUR", trip.Currency);
    }

    [TestMethod]
    public void ObjectMother_ActiveTripWithThreeParticipants_HasExpectedShape()
    {
        var trip = ObjectMother.ActiveTripWithThreeParticipants();

        Assert.AreEqual(TripStatus.Active, trip.Status);
        Assert.AreEqual(3, trip.ParticipantIds.Count);
    }
}
