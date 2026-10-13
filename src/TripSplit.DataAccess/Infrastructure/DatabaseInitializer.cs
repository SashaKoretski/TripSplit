using System.Reflection;

namespace TripSplit.DataAccess.Infrastructure;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;

    public DatabaseInitializer(IDbConnectionFactory factory) =>
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public async Task InitializeAsync(CancellationToken ct = default) =>
        await RunScriptAsync("init.sql", ct).ConfigureAwait(false);

    // Откатывает тестовое хранилище к состоянию "до прогона тестов" (ТИОПО ЛР2, Т4/Т10).
    // Использует тот же механизм последовательного запуска SQL-скрипта, что и InitializeAsync -
    // пригодно и для integration-, и для e2e-тестовых проектов без дублирования SQL.
    public async Task ResetAsync(CancellationToken ct = default) =>
        await RunScriptAsync("reset-test-data.sql", ct).ConfigureAwait(false);

    private async Task RunScriptAsync(string scriptName, CancellationToken ct)
    {
        var sql = await LoadEmbeddedSqlAsync(scriptName, ct).ConfigureAwait(false);
        await using var conn = await _factory.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<string> LoadEmbeddedSqlAsync(string name, CancellationToken ct)
    {
        var asm = typeof(DatabaseInitializer).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("." + name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded resource {name} not found");
        await using var stream = asm.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }
}