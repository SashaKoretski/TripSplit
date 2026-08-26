namespace TripSplit.BusinessLogic.Models.Exceptions;

public class ReceiptNotFoundException : Exception
{
    public ReceiptNotFoundException(Guid id)
        : base($"Receipt '{id}' not found") { }
}
