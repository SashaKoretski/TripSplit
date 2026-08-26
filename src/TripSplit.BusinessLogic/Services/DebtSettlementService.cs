using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.BusinessLogic.Services;

public class DebtSettlementService : IDebtSettlementService
{
    private readonly ITripRepository _trips;
    private readonly IExpenseRepository _expenses;
    private readonly IDebtMinimizationStrategy _strategy;

    public DebtSettlementService(
        ITripRepository trips,
        IExpenseRepository expenses,
        IDebtMinimizationStrategy strategy)
    {
        _trips = trips ?? throw new ArgumentNullException(nameof(trips));
        _expenses = expenses ?? throw new ArgumentNullException(nameof(expenses));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    // Собирает балансы по тратам, делегирует минимизацию стратегии и возвращает итоговую статистику
    public async Task<TripStatistics> CalculateSettlementAsync(Guid tripId)
    {
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));

        _ = await _trips.GetByIdAsync(tripId)
            ?? throw new TripNotFoundException(tripId);

        var expenses = await _expenses.GetByTripAsync(tripId);

        if (expenses.Count == 0)
        {
            return new TripStatistics(
                tripId, 0m,
                new Dictionary<Guid, decimal>(),
                Array.Empty<Transfer>());
        }

        var totalSpent = expenses.Sum(e => e.EffectiveAmount);
        var perUserSpent = ComputePerUserSpent(expenses);
        var balances = ComputeBalances(expenses);
        var transfers = _strategy.Minimize(balances);

        return new TripStatistics(tripId, totalSpent, perUserSpent, transfers);
    }

    // Сколько каждый пользователь заплатил
    private static IReadOnlyDictionary<Guid, decimal> ComputePerUserSpent(IEnumerable<Expense> expenses) =>
        expenses
            .GroupBy(e => e.PayerId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.EffectiveAmount));

    // Баланс участника = сколько он заплатил − его доля потребления по всем тратам
    private static IReadOnlyDictionary<Guid, decimal> ComputeBalances(IEnumerable<Expense> expenses)
    {
        var balances = new Dictionary<Guid, decimal>();

        foreach (var expense in expenses)
        {
            Add(balances, expense.PayerId, expense.EffectiveAmount);

            var share = expense.EffectiveAmount / expense.ConsumerIds.Count;
            foreach (var consumerId in expense.ConsumerIds)
                Add(balances, consumerId, -share);
        }

        return balances;
    }

    private static void Add(Dictionary<Guid, decimal> balances, Guid userId, decimal amount)
    {
        balances[userId] = balances.TryGetValue(userId, out var existing) ? existing + amount : amount;
    }
}
