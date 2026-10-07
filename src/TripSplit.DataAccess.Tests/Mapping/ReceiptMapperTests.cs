using TripSplit.DataAccess.Mapping;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Mapping;

/// <summary>
/// Unit-тест маппинга строки receipts на доменный Receipt (без реальной БД).
/// Техника данных: граничное значение даты (DateOnly.MinValue) как частный случай формата.
/// </summary>
public sealed class ReceiptMapperTests
{
    private static Dictionary<string, object?> Row(Guid id, Guid tripId, DateOnly date) => new()
    {
        ["id"] = id,
        ["trip_id"] = tripId,
        ["name"] = "Кафе",
        ["date"] = date,
    };

    [Fact]
    public void Map_ValidRow_ReturnsReceiptWithAllFields()
    {
        var id = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var date = new DateOnly(2026, 8, 25);
        var reader = new FakeDbDataReader(new[] { Row(id, tripId, date) });
        reader.Read();

        var receipt = ReceiptMapper.Map(reader);

        Assert.Equal(id, receipt.Id);
        Assert.Equal(tripId, receipt.TripId);
        Assert.Equal("Кафе", receipt.Name);
        Assert.Equal(date, receipt.Date);
    }

    [Fact]
    public void Map_MinValueDate_IsPreservedExactly()
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), Guid.NewGuid(), DateOnly.MinValue) });
        reader.Read();

        var receipt = ReceiptMapper.Map(reader);

        Assert.Equal(DateOnly.MinValue, receipt.Date);
    }
}
