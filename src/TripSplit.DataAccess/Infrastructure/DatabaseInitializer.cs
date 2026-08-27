using System.Reflection;

namespace TripSplit.DataAccess.Infrastructure;

public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;

    public DatabaseInitializer(IDbConnectionFactory factory) => _factory = factory;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var sql = await LoadEmbeddedSqlAsync("init.sql", ct).ConfigureAwait(false);
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