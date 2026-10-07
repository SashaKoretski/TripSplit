using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Mapping;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Mapping;

/// <summary>
/// Unit-тесты маппинга строки trips на доменный Trip (без реальной БД).
/// Техника данных: переходы состояний (active/finished) и граничное значение
/// (валюта с пробелами по краям, которую база хранит как CHAR(3)).
/// </summary>
public sealed class TripMapperTests
{
    private static Dictionary<string, object?> Row(Guid id, string status, string currency = "RUB") => new()
    {
        ["id"] = id,
        ["name"] = "Trip",
        ["status"] = status,
        ["currency"] = currency,
        ["created_at"] = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Theory]
    [InlineData("active", TripStatus.Active)]
    [InlineData("finished", TripStatus.Finished)]
    public void Map_KnownStatus_SetsCorrectTripStatus(string dbStatus, TripStatus expected)
    {
        var id = Guid.NewGuid();
        var reader = new FakeDbDataReader(new[] { Row(id, dbStatus) });
        reader.Read();

        var trip = TripMapper.Map(reader, Array.Empty<Guid>());

        Assert.Equal(id, trip.Id);
        Assert.Equal(expected, trip.Status);
    }

    [Fact]
    public void Map_UnknownStatus_ThrowsInvalidOperationException()
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), "cancelled") });
        reader.Read();

        Assert.Throws<InvalidOperationException>(() => TripMapper.Map(reader, Array.Empty<Guid>()));
    }

    [Fact]
    public void Map_CurrencyPaddedByFixedWidthColumn_IsTrimmed()
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), "active", "RUB") });
        reader.Read();

        var trip = TripMapper.Map(reader, Array.Empty<Guid>());

        Assert.Equal("RUB", trip.Currency);
    }

    [Fact]
    public void Map_WithParticipantIds_AttachesThemAll()
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), "active") });
        reader.Read();
        var participants = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var trip = TripMapper.Map(reader, participants);

        Assert.Equal(participants.OrderBy(g => g), trip.ParticipantIds.OrderBy(g => g));
    }

    [Theory]
    [InlineData(TripStatus.Active, "active")]
    [InlineData(TripStatus.Finished, "finished")]
    public void ToDbStatus_KnownValues_MapsToExpectedString(TripStatus status, string expected) =>
        Assert.Equal(expected, TripMapper.ToDbStatus(status));

    [Fact]
    public void ToDbStatus_UndefinedEnumValue_ThrowsArgumentOutOfRangeException() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TripMapper.ToDbStatus((TripStatus)999));
}
