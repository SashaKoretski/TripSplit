namespace TripSplit.DataAccess.Infrastructure.Exceptions;

public class StorageException : Exception
{
    public StorageException(string message, Exception inner) : base(message, inner) { }
}
