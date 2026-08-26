namespace TripSplit.BusinessLogic.Models;

// Итог по поездке: общая сумма, кто сколько потратил и список переводов
public class TripStatistics
{
    public Guid TripId { get; init; }
    public decimal TotalSpent { get; init; }
    public IReadOnlyDictionary<Guid, decimal> PerUserSpent { get; init; }
    public IReadOnlyList<Transfer> Transfers { get; init; }

    public TripStatistics(
        Guid tripId,
        decimal totalSpent,
        IReadOnlyDictionary<Guid, decimal> perUserSpent,
        IReadOnlyList<Transfer> transfers)
    {
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));

        TripId = tripId;
        TotalSpent = totalSpent;
        PerUserSpent = perUserSpent ?? throw new ArgumentNullException(nameof(perUserSpent));
        Transfers = transfers ?? throw new ArgumentNullException(nameof(transfers));
    }
}
