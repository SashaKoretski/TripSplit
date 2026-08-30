using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class ReceiptListVm
{
    public Trip Trip { get; init; } = default!;
    public IReadOnlyList<Receipt> Receipts { get; init; } = Array.Empty<Receipt>();
}
