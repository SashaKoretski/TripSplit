using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/UserSpent, Transfer, Settlement
public class UserSpentDto
{
    public Guid UserId { get; init; }
    public decimal Amount { get; init; }
}

public class TransferDto
{
    public Guid FromUserId { get; init; }
    public Guid ToUserId { get; init; }
    public decimal Amount { get; init; }
}

public class SettlementDto
{
    public Guid TripId { get; init; }
    public decimal TotalSpent { get; init; }
    public required IReadOnlyList<UserSpentDto> PerUserSpent { get; init; }
    public required IReadOnlyList<TransferDto> Transfers { get; init; }

    public static SettlementDto FromDomain(TripStatistics stats) => new()
    {
        TripId = stats.TripId,
        TotalSpent = stats.TotalSpent,
        PerUserSpent = stats.PerUserSpent
            .Select(kv => new UserSpentDto { UserId = kv.Key, Amount = kv.Value })
            .ToList(),
        Transfers = stats.Transfers
            .Select(t => new TransferDto { FromUserId = t.FromUserId, ToUserId = t.ToUserId, Amount = t.Amount })
            .ToList()
    };
}
