namespace TripSplit.BusinessLogic.Models.Exceptions;

public class ExpenseNotFoundException : Exception
{
    public ExpenseNotFoundException(Guid id)
        : base($"Expense '{id}' not found") { }
}
