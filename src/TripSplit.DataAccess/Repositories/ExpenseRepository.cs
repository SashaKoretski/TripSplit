using Npgsql;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Mapping;

namespace TripSplit.DataAccess.Repositories;

public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly IDbConnectionFactory _factory;

    public ExpenseRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Expense?> GetByIdAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        await using var conn = await _factory.OpenAsync();
        try
        {
            var row = await LoadRowAsync(conn, id);
            if (row is null) return null;
            var consumers = (await LoadConsumersAsync(conn, new[] { id }))
                .GetValueOrDefault(id, new List<Guid>());
            return ExpenseMapper.ToDomain(row.Value, consumers);
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load expense: " + e.MessageText, e);
        }
    }

    public async Task<IReadOnlyList<Expense>> GetByTripAsync(Guid tripId)
    {
        RequireNotEmpty(tripId, nameof(tripId));
        await using var conn = await _factory.OpenAsync();
        try
        {
            var rows = new List<ExpenseMapper.Row>();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT {ExpenseMapper.Columns} FROM expenses WHERE trip_id = @tid";
                AddParameter(cmd, "tid", tripId);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) rows.Add(ExpenseMapper.ReadRow(reader));
            }
            var consumers = await LoadConsumersAsync(conn, rows.Select(r => r.Id).ToArray());
            return rows
                .Select(r => ExpenseMapper.ToDomain(r, consumers.GetValueOrDefault(r.Id, new List<Guid>())))
                .ToList();
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load expenses: " + e.MessageText, e);
        }
    }

    public async Task AddAsync(Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        await using var conn = await _factory.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await ExecuteInTxAsync(conn, tx,
                "INSERT INTO expenses (id, trip_id, receipt_id, payer_id, name, type, value, discount) " +
                "VALUES (@id, @tid, @rid, @pid, @name, @type, @value, @disc)",
                ("id", expense.Id),
                ("tid", expense.TripId),
                ("rid", (object?)expense.ReceiptId ?? DBNull.Value),
                ("pid", expense.PayerId),
                ("name", expense.Name),
                ("type", ExpenseMapper.ToDbType(expense.Type)),
                ("value", expense.Value),
                ("disc", expense.Discount));
            await ReplaceConsumersAsync(conn, tx, expense.Id, expense.ConsumerIds);
            await tx.CommitAsync();
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync();
            throw new DuplicateKeyException("Expense already exists", e);
        }
        catch (PostgresException e)
        {
            await tx.RollbackAsync();
            throw new RepositoryException("Failed to insert expense: " + e.MessageText, e);
        }
    }

    public async Task UpdateAsync(Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        await using var conn = await _factory.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var rows = await ExecuteInTxAsync(conn, tx,
                "UPDATE expenses SET receipt_id = @rid, payer_id = @pid, name = @name, " +
                "type = @type, value = @value, discount = @disc, updated_at = now() WHERE id = @id",
                ("id", expense.Id),
                ("rid", (object?)expense.ReceiptId ?? DBNull.Value),
                ("pid", expense.PayerId),
                ("name", expense.Name),
                ("type", ExpenseMapper.ToDbType(expense.Type)),
                ("value", expense.Value),
                ("disc", expense.Discount));
            if (rows == 0)
            {
                await tx.RollbackAsync();
                throw new EntityNotFoundException("Expense", expense.Id);
            }
            await ReplaceConsumersAsync(conn, tx, expense.Id, expense.ConsumerIds);
            await tx.CommitAsync();
        }
        catch (PostgresException e)
        {
            await tx.RollbackAsync();
            throw new RepositoryException("Failed to update expense: " + e.MessageText, e);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        await using var conn = await _factory.OpenAsync();
        int rows;
        try
        {
            rows = await ExecuteAsync(conn, "DELETE FROM expenses WHERE id = @id", ("id", id));
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to delete expense: " + e.MessageText, e);
        }
        if (rows == 0) throw new EntityNotFoundException("Expense", id);
    }

    private static async Task<ExpenseMapper.Row?> LoadRowAsync(DbConnection conn, Guid id)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {ExpenseMapper.Columns} FROM expenses WHERE id = @id";
        AddParameter(cmd, "id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ExpenseMapper.ReadRow(reader) : (ExpenseMapper.Row?)null;
    }

    private static async Task<Dictionary<Guid, List<Guid>>> LoadConsumersAsync(DbConnection conn, Guid[] expenseIds)
    {
        var result = new Dictionary<Guid, List<Guid>>();
        if (expenseIds.Length == 0) return result;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT expense_id, user_id FROM expense_consumers WHERE expense_id = ANY(@ids)";
        AddParameter(cmd, "ids", expenseIds);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var eid = reader.GetGuid(0);
            var uid = reader.GetGuid(1);
            if (!result.TryGetValue(eid, out var list))
                result[eid] = list = new List<Guid>();
            list.Add(uid);
        }
        return result;
    }

    private static async Task ReplaceConsumersAsync(DbConnection conn, DbTransaction tx, Guid expenseId, IReadOnlyList<Guid> userIds)
    {
        await ExecuteInTxAsync(conn, tx,
            "DELETE FROM expense_consumers WHERE expense_id = @id", ("id", expenseId));
        foreach (var uid in userIds.Distinct())
            await ExecuteInTxAsync(conn, tx,
                "INSERT INTO expense_consumers (expense_id, user_id) VALUES (@e, @u)",
                ("e", expenseId), ("u", uid));
    }

    private static async Task<int> ExecuteAsync(DbConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in parameters) AddParameter(cmd, n, v);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> ExecuteInTxAsync(DbConnection conn, DbTransaction tx, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
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
