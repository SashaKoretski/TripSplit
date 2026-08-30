using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class ExpenseRowVm
{
    public Expense Expense { get; init; } = default!;
    public string PayerName { get; init; } = string.Empty;
    public IReadOnlyList<string> ConsumerNames { get; init; } = Array.Empty<string>();
}

public class ExpenseListVm
{
    public Trip Trip { get; init; } = default!;
    public IReadOnlyList<ExpenseRowVm> Expenses { get; init; } = Array.Empty<ExpenseRowVm>();
    public decimal Total { get; init; }
}
