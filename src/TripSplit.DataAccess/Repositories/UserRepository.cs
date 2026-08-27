using Npgsql;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Mapping;

namespace TripSplit.DataAccess.Repositories;
public sealed class UserRepository : IUserRepository
{
    private const string Columns = "id, name, email, google_id";
    private readonly IDbConnectionFactory _factory;

    public UserRepository(IDbConnectionFactory factory) => _factory = factory;

    public Task<User?> GetByIdAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        return FindSingleAsync($"SELECT {Columns} FROM users WHERE id = @id", ("id", id));
    }

    public Task<User?> GetByGoogleIdAsync(string googleId)
    {
        RequireNotBlank(googleId, nameof(googleId));
        return FindSingleAsync($"SELECT {Columns} FROM users WHERE google_id = @gid", ("gid", googleId));
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        RequireNotBlank(email, nameof(email));
        return FindSingleAsync($"SELECT {Columns} FROM users WHERE email = @email", ("email", email));
    }

    public async Task AddAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        const string sql = "INSERT INTO users(id, name, email, google_id) VALUES (@id, @name, @email, @gid)";
        try
        {
            await ExecuteAsync(sql,
                ("id", user.Id), ("name", user.Name), ("email", user.Email), ("gid", user.GoogleId));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateKeyException("User with same email/google_id already exists", e);
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to insert user: " + e.MessageText, e);
        }
    }

    public async Task UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        const string sql = "UPDATE users SET name = @name, email = @email, google_id = @gid WHERE id = @id";
        int rows;
        try
        {
            rows = await ExecuteAsync(sql,
                ("id", user.Id), ("name", user.Name), ("email", user.Email), ("gid", user.GoogleId));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateKeyException("User with same email/google_id already exists", e);
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to update user: " + e.MessageText, e);
        }
        if (rows == 0) throw new EntityNotFoundException("User", user.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        int rows;
        try
        {
            rows = await ExecuteAsync("DELETE FROM users WHERE id = @id", ("id", id));
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to delete user: " + e.MessageText, e);
        }
        if (rows == 0) throw new EntityNotFoundException("User", id);
    }

    private async Task<User?> FindSingleAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var conn = await _factory.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddParameters(cmd, parameters);
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? UserMapper.Map(reader) : null;
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to query users: " + e.MessageText, e);
        }
    }

    private async Task<int> ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var conn = await _factory.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddParameters(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    private static void AddParameters(DbCommand cmd, (string Name, object Value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }

    private static void RequireNotEmpty(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty", name);
    }

    private static void RequireNotBlank(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be blank", name);
    }
}