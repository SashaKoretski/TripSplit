namespace TripSplit.BusinessLogic.Models.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid id)
        : base($"User '{id}' not found") { }
}
