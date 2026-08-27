namespace TripSplit.DataAccess.Infrastructure.Exceptions;

public sealed class DuplicateKeyException : RepositoryException
{
    public DuplicateKeyException(string message, Exception inner) : base(message, inner) { }
}