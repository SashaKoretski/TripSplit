namespace TripSplit.DataAccess.Infrastructure.Exceptions;

public sealed class EntityNotFoundException : RepositoryException
{
    public EntityNotFoundException(string entity, Guid id)
        : base($"{entity} with id={id} not found") { }
}