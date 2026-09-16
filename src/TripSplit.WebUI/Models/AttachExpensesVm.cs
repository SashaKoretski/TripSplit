using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class AttachExpensesVm
{
    public Trip Trip { get; init; } = default!;
    public Receipt Receipt { get; init; } = default!;
    public IReadOnlyList<Expense> UnlinkedExpenses { get; init; } = Array.Empty<Expense>();
}
