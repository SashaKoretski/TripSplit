namespace TripSplit.BusinessLogic.Models.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid id)
        : base($"User '{id}' not found") { }

    public UserNotFoundException(string email)
        : base($"User with email '{email}' not found") { }
}
