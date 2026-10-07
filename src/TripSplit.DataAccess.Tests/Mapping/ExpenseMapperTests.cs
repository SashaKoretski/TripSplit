using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Mapping;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Mapping;

/// <summary>
/// Unit-тесты маппинга строки БД на доменный объект Expense (без реальной БД).
/// Техника данных: классы эквивалентности (receipt_id присутствует / NULL) и
/// комбинаторный перебор всех значений перечисления ExpenseType для ToDbType/ReadRow.
/// </summary>
public sealed class ExpenseMapperTests
{
    private static Dictionary<string, object?> Row(Guid id, Guid? receiptId, string type) => new()
    {
        ["id"] = id,
        ["trip_id"] = Guid.NewGuid(),
        ["receipt_id"] = receiptId,
        ["payer_id"] = Guid.NewGuid(),
        ["name"] = "Dinner",
        ["type"] = type,
        ["value"] = 100m,
        ["discount"] = 10m,
    };

    [Fact]
    public void ReadRow_WithReceiptId_MapsReceiptId()
    {
        var id = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var reader = new FakeDbDataReader(new[] { Row(id, receiptId, "food") });
        reader.Read();

        var row = ExpenseMapper.ReadRow(reader);

        Assert.Equal(id, row.Id);
        Assert.Equal(receiptId, row.ReceiptId);
        Assert.Equal(ExpenseType.Food, row.Type);
    }

    [Fact]
    public void ReadRow_NullReceiptId_MapsNull()
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), null, "other") });
        reader.Read();

        var row = ExpenseMapper.ReadRow(reader);

        Assert.Null(row.ReceiptId);
    }

    [Fact]
    public void ReadRow_UnknownTypeString_ThrowsInvalidOperationException()
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), null, "unknown-type") });
        reader.Read();

        Assert.Throws<InvalidOperationException>(() => ExpenseMapper.ReadRow(reader));
    }

    [Theory]
    [InlineData(ExpenseType.Food, "food")]
    [InlineData(ExpenseType.Transport, "transport")]
    [InlineData(ExpenseType.Accommodation, "accommodation")]
    [InlineData(ExpenseType.Entertainment, "entertainment")]
    [InlineData(ExpenseType.Other, "other")]
    public void ToDbType_EachEnumValue_RoundTripsThroughReadRow(ExpenseType type, string dbValue)
    {
        Assert.Equal(dbValue, ExpenseMapper.ToDbType(type));

        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), null, dbValue) });
        reader.Read();
        Assert.Equal(type, ExpenseMapper.ReadRow(reader).Type);
    }

    [Fact]
    public void ToDbType_UndefinedEnumValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ExpenseMapper.ToDbType((ExpenseType)999));
    }

    [Fact]
    public void ToDomain_BuildsExpenseWithGivenConsumers()
    {
        var id = Guid.NewGuid();
        var reader = new FakeDbDataReader(new[] { Row(id, null, "food") });
        reader.Read();
        var row = ExpenseMapper.ReadRow(reader);
        var consumerIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var expense = ExpenseMapper.ToDomain(row, consumerIds);

        Assert.Equal(id, expense.Id);
        Assert.Equal(consumerIds.OrderBy(g => g), expense.ConsumerIds.OrderBy(g => g));
        Assert.Equal(90m, expense.EffectiveAmount);
    }
}
