using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

// Стратегия минимизации количества переводов между участниками
public interface IDebtMinimizationStrategy
{
    // На входе баланс каждого участника (+ кредитор, − должник), на выходе список переводов
    IReadOnlyList<Transfer> Minimize(IReadOnlyDictionary<Guid, decimal> balances);
}
