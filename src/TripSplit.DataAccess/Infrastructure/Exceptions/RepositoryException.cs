namespace TripSplit.DataAccess.Infrastructure.Exceptions;

public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception inner) : base(message, inner) { }
}