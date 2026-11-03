using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TripSplit.DataAccess.Infrastructure;

namespace TripSplit.WebUI.Tests;

// Отдельная БД (tripsplit_test), НЕ tripsplit_db - интеграционные тесты не должны
// трогать данные разработчика. Переопределяется TRIPSPLIT_TEST_DB, как и в
// TripSplit.DataAccess.Tests.DatabaseFixture (тот же конвенционный env var).
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TRIPSPLIT_TEST_DB")
        ?? "Host=localhost;Database=tripsplit_test;Username=postgres;Password=12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = ConnectionString
            });
        });
    }

    public async Task InitializeSchemaAsync()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        await new DatabaseInitializer(factory).InitializeAsync();
    }

    public async Task ResetDataAsync()
    {
        var factory = new NpgsqlConnectionFactory(ConnectionString);
        await using var conn = await factory.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "TRUNCATE expense_consumers, expenses, receipt_images, receipts, trip_participants, trips, users " +
            "RESTART IDENTITY CASCADE";
        await cmd.ExecuteNonQueryAsync();
    }
}
