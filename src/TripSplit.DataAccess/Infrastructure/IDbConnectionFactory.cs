namespace TripSplit.DataAccess.Infrastructure;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken ct = default);
}