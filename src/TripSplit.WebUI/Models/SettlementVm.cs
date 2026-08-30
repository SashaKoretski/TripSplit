using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class UserSpentVm
{
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public class SettlementRowVm
{
    public string FromName { get; init; } = string.Empty;
    public string ToName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public class SettlementVm
{
    public Trip Trip { get; init; } = default!;
    public decimal Total { get; init; }
    public IReadOnlyList<UserSpentVm> PerUser { get; init; } = Array.Empty<UserSpentVm>();
    public IReadOnlyList<SettlementRowVm> Transfers { get; init; } = Array.Empty<SettlementRowVm>();
}
