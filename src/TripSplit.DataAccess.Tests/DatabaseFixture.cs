using TripSplit.DataAccess.Infrastructure;
using Xunit;

namespace TripSplit.DataAccess.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TRIPSPLIT_TEST_DB")
        ?? "Host=localhost;Database=tripsplit_db;Username=postgres;Password=12345";

    public IDbConnectionFactory Factory { get; } = new NpgsqlConnectionFactory(ConnectionString);

    public async Task InitializeAsync()
    {
        var init = new DatabaseInitializer(Factory);
        await init.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task ResetAsync()
    {
        await using var conn = await Factory.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "TRUNCATE expense_consumers, expenses, receipt_images, receipts, trip_participants, trips, users " +
            "RESTART IDENTITY CASCADE";
        await cmd.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }