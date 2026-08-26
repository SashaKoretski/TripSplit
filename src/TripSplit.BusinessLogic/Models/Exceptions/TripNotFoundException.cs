namespace TripSplit.BusinessLogic.Models.Exceptions;

public class TripNotFoundException : Exception
{
    public TripNotFoundException(Guid id)
        : base($"Trip '{id}' not found") { }
}
