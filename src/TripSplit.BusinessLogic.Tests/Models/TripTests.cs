using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests.Models;

/// <summary>
/// Классический (без mock/stub) unit-тест доменной модели Trip: Trip не имеет зависимостей,
/// поэтому Arrange/Act/Assert работают с реальным объектом напрямую (Т3 — "классический" вариант).
/// Техника данных: граничные значения (длина кода валюты 2/3/4 символа) и классы эквивалентности
/// (пустое/из пробелов имя).
/// </summary>
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
    public void Ctor_EmptyId_ThrowsArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Trip(Guid.Empty, "Trip", "RUB", DateTime.UtcNow));

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Ctor_BlankName_ThrowsArgumentException(string name) =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Trip(Guid.NewGuid(), name, "RUB", DateTime.UtcNow));

    // Техника: граничное значение длины кода валюты (2, 3 — валидно, 4 — вне границы)
    [TestMethod]
    [DataRow("RU")]
    [DataRow("RUBL")]
    [DataRow("")]
    public void Ctor_CurrencyLengthNot3_ThrowsArgumentException(string currency) =>
        Assert.ThrowsExactly<ArgumentException>(() => _ = new Trip(Guid.NewGuid(), "Trip", currency, DateTime.UtcNow));

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
