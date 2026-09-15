namespace TripSplit.BusinessLogic.Models.Exceptions;

public class ReceiptImageNotFoundException : Exception
{
    public ReceiptImageNotFoundException(Guid receiptId)
        : base($"Image for receipt '{receiptId}' not found") { }
}
