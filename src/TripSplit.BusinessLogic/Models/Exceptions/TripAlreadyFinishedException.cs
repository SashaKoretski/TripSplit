namespace TripSplit.BusinessLogic.Models.Exceptions;

public class TripAlreadyFinishedException : Exception
{
    public TripAlreadyFinishedException(Guid tripId)
        : base($"Trip '{tripId}' is already finished") { }
}
