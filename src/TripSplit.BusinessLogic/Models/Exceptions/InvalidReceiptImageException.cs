namespace TripSplit.BusinessLogic.Models.Exceptions;

public class InvalidReceiptImageException : Exception
{
    public InvalidReceiptImageException(string message) : base(message) { }
}
