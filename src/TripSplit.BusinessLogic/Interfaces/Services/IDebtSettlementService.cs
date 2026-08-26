using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

public interface IDebtSettlementService
{
    // Считает балансы по тратам поездки и возвращает план погашения долгов
    Task<TripStatistics> CalculateSettlementAsync(Guid tripId);
}
