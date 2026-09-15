using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class ReceiptListVm
{
    public Trip Trip { get; init; } = default!;
    public IReadOnlyList<ReceiptRowVm> Receipts { get; init; } = Array.Empty<ReceiptRowVm>();
}

public class ReceiptRowVm
{
    public Receipt Receipt { get; init; } = default!;
    public string? ImageUrl { get; init; }
    public string? ImageFileName { get; init; }
    public IReadOnlyList<Expense> LinkedExpenses { get; init; } = Array.Empty<Expense>();
}
