namespace TripSplit.BusinessLogic.Models.Exceptions;

public class InvalidExpenseException : Exception
{
    public InvalidExpenseException(string message) : base(message) { }
}
