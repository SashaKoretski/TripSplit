using Npgsql;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Mapping;

namespace TripSplit.DataAccess.Repositories;

public sealed class TripRepository : ITripRepository
{
    private readonly IDbConnectionFactory _factory;

    public TripRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Trip?> GetByIdAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        await using var conn = await _factory.OpenAsync();
        try
        {
            var trips = await LoadTripsAsync(conn,
                $"SELECT {TripMapper.Columns} FROM trips WHERE id = @id",
                ("id", id));
            var attached = await AttachParticipantsAsync(conn, trips);
            return attached.Count > 0 ? attached[0] : null;
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load trip: " + e.MessageText, e);
        }
    }

    public async Task<IReadOnlyList<Trip>> GetAllAsync()
    {
        await using var conn = await _factory.OpenAsync();
        try
        {
            var trips = await LoadTripsAsync(conn, $"SELECT {TripMapper.Columns} FROM trips");
            return await AttachParticipantsAsync(conn, trips);
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load trips: " + e.MessageText, e);
        }
    }

    public async Task<IReadOnlyList<Trip>> GetByUserAsync(Guid userId)
    {
        RequireNotEmpty(userId, nameof(userId));
        await using var conn = await _factory.OpenAsync();
        try
        {
            var trips = await LoadTripsAsync(conn,
                $"SELECT {TripMapper.Columns} FROM trips WHERE id IN " +
                "(SELECT trip_id FROM trip_participants WHERE user_id = @uid)",
                ("uid", userId));
            return await AttachParticipantsAsync(conn, trips);
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load user trips: " + e.MessageText, e);
        }
    }

    public async Task AddAsync(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        await using var conn = await _factory.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await ExecuteInTxAsync(conn, tx,
                "INSERT INTO trips (id, name, status, currency, created_at, updated_at) " +
                "VALUES (@id, @name, @status, @cur, @created, @created)",
                ("id", trip.Id),
                ("name", trip.Name),
                ("status", TripMapper.ToDbStatus(trip.Status)),
                ("cur", trip.Currency),
                ("created", trip.CreatedAt));
            await ReplaceParticipantsAsync(conn, tx, trip.Id, trip.ParticipantIds);
            await tx.CommitAsync();
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync();
            throw new DuplicateKeyException("Trip already exists", e);
        }
        catch (PostgresException e)
        {
            await tx.RollbackAsync();
            throw new RepositoryException("Failed to insert trip: " + e.MessageText, e);
        }
    }

    public async Task UpdateAsync(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        await using var conn = await _factory.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var rows = await ExecuteInTxAsync(conn, tx,
                "UPDATE trips SET name = @name, status = @status, currency = @cur, updated_at = now() " +
                "WHERE id = @id",
                ("id", trip.Id),
                ("name", trip.Name),
                ("status", TripMapper.ToDbStatus(trip.Status)),
                ("cur", trip.Currency));
            if (rows == 0)
            {
                await tx.RollbackAsync();
                throw new EntityNotFoundException("Trip", trip.Id);
            }
            await ReplaceParticipantsAsync(conn, tx, trip.Id, trip.ParticipantIds);
            await tx.CommitAsync();
        }
        catch (PostgresException e)
        {
            await tx.RollbackAsync();
            throw new RepositoryException("Failed to update trip: " + e.MessageText, e);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        await using var conn = await _factory.OpenAsync();
        int rows;
        try
        {
            rows = await ExecuteAsync(conn, "DELETE FROM trips WHERE id = @id", ("id", id));
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to delete trip: " + e.MessageText, e);
        }
        if (rows == 0) throw new EntityNotFoundException("Trip", id);
    }

    private static async Task<List<Trip>> LoadTripsAsync(DbConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in parameters) AddParameter(cmd, n, v);
        var result = new List<Trip>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(TripMapper.Map(reader, Array.Empty<Guid>()));
        return result;
    }

    private static async Task<IReadOnlyList<Trip>> AttachParticipantsAsync(DbConnection conn, List<Trip> trips)
    {
        if (trips.Count == 0) return trips;
        var participants = await LoadParticipantsAsync(conn, trips.Select(t => t.Id).ToArray());
        foreach (var t in trips)
            if (participants.TryGetValue(t.Id, out var list))
                foreach (var pid in list) t.ParticipantIds.Add(pid);
        return trips;
    }

    private static async Task<Dictionary<Guid, List<Guid>>> LoadParticipantsAsync(DbConnection conn, Guid[] tripIds)
    {
        var result = new Dictionary<Guid, List<Guid>>();
        if (tripIds.Length == 0) return result;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT trip_id, user_id FROM trip_participants WHERE trip_id = ANY(@ids)";
        AddParameter(cmd, "ids", tripIds);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tid = reader.GetGuid(0);
            var uid = reader.GetGuid(1);
            if (!result.TryGetValue(tid, out var list))
                result[tid] = list = new List<Guid>();
            list.Add(uid);
        }
        return result;
    }

    private static async Task ReplaceParticipantsAsync(DbConnection conn, DbTransaction tx, Guid tripId, IReadOnlyList<Guid> userIds)
    {
        await ExecuteInTxAsync(conn, tx,
            "DELETE FROM trip_participants WHERE trip_id = @id", ("id", tripId));
        foreach (var uid in userIds.Distinct())
            await ExecuteInTxAsync(conn, tx,
                "INSERT INTO trip_participants (trip_id, user_id) VALUES (@t, @u)",
                ("t", tripId), ("u", uid));
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
