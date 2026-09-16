using Npgsql;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Mapping;

namespace TripSplit.DataAccess.Repositories;

public sealed class ReceiptRepository : IReceiptRepository
{
    private readonly IDbConnectionFactory _factory;

    public ReceiptRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Receipt?> GetByIdAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        return await FindSingleAsync(
            $"SELECT {ReceiptMapper.Columns} FROM receipts WHERE id = @id",
            ("id", id));
    }

    public async Task<IReadOnlyList<Receipt>> GetByTripAsync(Guid tripId)
    {
        RequireNotEmpty(tripId, nameof(tripId));
        await using var conn = await _factory.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {ReceiptMapper.Columns} FROM receipts WHERE trip_id = @tid";
            AddParameter(cmd, "tid", tripId);
            var result = new List<Receipt>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Add(ReceiptMapper.Map(reader));
            return result;
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load receipts: " + e.MessageText, e);
        }
    }

    public async Task AddAsync(Receipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        const string sql = "INSERT INTO receipts (id, trip_id, name, date) VALUES (@id, @tid, @name, @date)";
        try
        {
            await ExecuteAsync(sql,
                ("id", receipt.Id),
                ("tid", receipt.TripId),
                ("name", receipt.Name),
                ("date", receipt.Date));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateKeyException("Receipt already exists", e);
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to insert receipt: " + e.MessageText, e);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        int rows;
        try
        {
            rows = await ExecuteAsync("DELETE FROM receipts WHERE id = @id", ("id", id));
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to delete receipt: " + e.MessageText, e);
        }
        if (rows == 0) throw new EntityNotFoundException("Receipt", id);
    }

    private async Task<Receipt?> FindSingleAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var conn = await _factory.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (n, v) in parameters) AddParameter(cmd, n, v);
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReceiptMapper.Map(reader) : null;
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load receipt: " + e.MessageText, e);
        }
    }

    private async Task<int> ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var conn = await _factory.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in parameters) AddParameter(cmd, n, v);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private static void RequireNotEmpty(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty", name);
    }
}
