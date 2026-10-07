using TripSplit.DataAccess.Mapping;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Mapping;

/// <summary>
/// Истинно изолированные unit-тесты компонента доступа к данным: строка результата запроса
/// подделывается в памяти (FakeDbDataReader), реальная БД не требуется и не используется.
/// Техника подготовки данных: классы эквивалентности полей строки (валидная строка / null в nullable-поле).
/// </summary>
public sealed class UserMapperTests
{
    private static Dictionary<string, object?> ValidRow(Guid? id = null) => new()
    {
        ["id"] = id ?? Guid.NewGuid(),
        ["name"] = "Sasha",
        ["email"] = "sasha@example.test",
        ["google_id"] = "gid-sasha",
    };

    [Fact]
    public void Map_ValidRow_ReturnsUserWithAllFields()
    {
        var id = Guid.NewGuid();
        var reader = new FakeDbDataReader(new[] { ValidRow(id) });
        reader.Read();

        var user = UserMapper.Map(reader);

        Assert.Equal(id, user.Id);
        Assert.Equal("Sasha", user.Name);
        Assert.Equal("sasha@example.test", user.Email);
        Assert.Equal("gid-sasha", user.GoogleId);
    }

    [Fact]
    public void Map_MissingColumn_ThrowsIndexOutOfRangeException()
    {
        var row = ValidRow();
        row.Remove("google_id");
        var reader = new FakeDbDataReader(new[] { row });
        reader.Read();

        Assert.Throws<IndexOutOfRangeException>(() => UserMapper.Map(reader));
    }
}
