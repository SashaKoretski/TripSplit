using TripSplit.DataAccess.Infrastructure;
using Xunit;

namespace TripSplit.DataAccess.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    // По умолчанию - отдельный тестовый инстанс Postgres из docker-compose.yml (service
    // postgres-test, порт 5433), а не dev-БД на 5432. Переопределяется TRIPSPLIT_TEST_DB
    // для параллельных/CI-прогонов с другим портом/именем БД (ТИОПО ЛР2, Т2-Т3, Т15).
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TRIPSPLIT_TEST_DB")
        ?? "Host=localhost;Port=5433;Database=tripsplit_test;Username=tripsplit_test;Password=tripsplit_test";

    public IDbConnectionFactory Factory { get; } = new NpgsqlConnectionFactory(ConnectionString);

    private readonly DatabaseInitializer _initializer;

    public DatabaseFixture() => _initializer = new DatabaseInitializer(Factory);

    public Task InitializeAsync() => _initializer.InitializeAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    public Task ResetAsync() => _initializer.ResetAsync();
}

[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }