using Npgsql;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Mapping;

namespace TripSplit.DataAccess.Repositories;

public sealed class ReceiptImageRepository : IReceiptImageRepository
{
    private readonly IDbConnectionFactory _factory;

    public ReceiptImageRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<ReceiptImage?> GetByReceiptAsync(Guid receiptId)
    {
        RequireNotEmpty(receiptId, nameof(receiptId));
        await using var conn = await _factory.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT {ReceiptImageMapper.Columns} FROM receipt_images WHERE receipt_id = @rid";
            AddParameter(cmd, "rid", receiptId);
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReceiptImageMapper.Map(reader) : null;
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to load receipt image: " + e.MessageText, e);
        }
    }

    public async Task AddAsync(ReceiptImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        const string sql = """
            INSERT INTO receipt_images (id, receipt_id, storage_key, content_type, file_size_bytes, original_file_name, uploaded_at)
            VALUES (@id, @rid, @key, @ctype, @size, @name, @uploaded)
            """;
        try
        {
            await ExecuteAsync(sql,
                ("id", image.Id),
                ("rid", image.ReceiptId),
                ("key", image.StorageKey),
                ("ctype", image.ContentType),
                ("size", image.FileSizeBytes),
                ("name", image.OriginalFileName),
                ("uploaded", image.UploadedAt));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateKeyException("Receipt image already exists", e);
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to insert receipt image: " + e.MessageText, e);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        RequireNotEmpty(id, nameof(id));
        int rows;
        try
        {
            rows = await ExecuteAsync("DELETE FROM receipt_images WHERE id = @id", ("id", id));
        }
        catch (PostgresException e)
        {
            throw new RepositoryException("Failed to delete receipt image: " + e.MessageText, e);
        }
        if (rows == 0) throw new EntityNotFoundException("ReceiptImage", id);
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
