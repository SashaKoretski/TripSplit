using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Infrastructure;

/// <summary>
/// Unit-тест идемпотентной инициализации схемы: embedded SQL-ресурс исполняется через
/// поддельное соединение, без реальной БД.
/// </summary>
public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_ExecutesEmbeddedInitSql()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var initializer = new DatabaseInitializer(new FakeDbConnectionFactory(conn));

        await initializer.InitializeAsync();

        var cmd = Assert.Single(conn.ExecutedCommands);
        Assert.Contains("CREATE TABLE", cmd.CommandText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ctor_NullFactory_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new DatabaseInitializer(null!));

    [Fact]
    public async Task InitializeAsync_CalledTwice_IsIdempotentAndRunsEachTime()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0), FakeCommandScript.NonQuery(0));
        var initializer = new DatabaseInitializer(new FakeDbConnectionFactory(conn));

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        Assert.Equal(2, conn.ExecutedCommands.Count);
        Assert.Equal(conn.ExecutedCommands[0].CommandText, conn.ExecutedCommands[1].CommandText);
    }
}
