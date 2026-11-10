using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Mapping;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Mapping;

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

        var act = () => { _ = ExpenseMapper.ReadRow(reader); };

        Assert.Throws<InvalidOperationException>(act);
    }

    [Theory]
    [InlineData(ExpenseType.Food, "food")]
    [InlineData(ExpenseType.Transport, "transport")]
    [InlineData(ExpenseType.Accommodation, "accommodation")]
    [InlineData(ExpenseType.Entertainment, "entertainment")]
    [InlineData(ExpenseType.Other, "other")]
    public void ToDbType_EachEnumValue_MapsToExpectedDbString(ExpenseType type, string dbValue)
    {
        var result = ExpenseMapper.ToDbType(type);

        Assert.Equal(dbValue, result);
    }

    [Theory]
    [InlineData(ExpenseType.Food, "food")]
    [InlineData(ExpenseType.Transport, "transport")]
    [InlineData(ExpenseType.Accommodation, "accommodation")]
    [InlineData(ExpenseType.Entertainment, "entertainment")]
    [InlineData(ExpenseType.Other, "other")]
    public void ReadRow_EachDbTypeString_RoundTripsToEnumValue(ExpenseType type, string dbValue)
    {
        var reader = new FakeDbDataReader(new[] { Row(Guid.NewGuid(), null, dbValue) });
        reader.Read();

        var row = ExpenseMapper.ReadRow(reader);

        Assert.Equal(type, row.Type);
    }

    [Fact]
    public void ToDbType_UndefinedEnumValue_ThrowsArgumentOutOfRangeException()
    {
        var act = () => ExpenseMapper.ToDbType((ExpenseType)999);

        Assert.Throws<ArgumentOutOfRangeException>(act);
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
