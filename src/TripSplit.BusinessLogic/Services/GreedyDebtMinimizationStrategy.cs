using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Services;

// Жадный двухуказательный алгоритм: самый крупный должник платит самому крупному кредитору
public class GreedyDebtMinimizationStrategy : IDebtMinimizationStrategy
{
    private readonly decimal _epsilon;

    public GreedyDebtMinimizationStrategy(DebtSettlementOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        if (options.MinTransferAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(options),
                "MinTransferAmount must be positive");
        _epsilon = options.MinTransferAmount;
    }

    public IReadOnlyList<Transfer> Minimize(IReadOnlyDictionary<Guid, decimal> balances)
    {
        if (balances is null)
            throw new ArgumentNullException(nameof(balances));

        if (balances.Count == 0)
            return Array.Empty<Transfer>();

        var sum = balances.Values.Sum();
        if (Math.Abs(sum) > _epsilon)
            throw new ArgumentException(
                $"Balances must sum to zero, got {sum}", nameof(balances));

        var debtors = balances
            .Where(kv => kv.Value < -_epsilon)
            .Select(kv => new Node(kv.Key, -kv.Value))
            .OrderByDescending(n => n.Remaining)
            .ToList();

        var creditors = balances
            .Where(kv => kv.Value > _epsilon)
            .Select(kv => new Node(kv.Key, kv.Value))
            .OrderByDescending(n => n.Remaining)
            .ToList();

        var transfers = new List<Transfer>();
        int i = 0, j = 0;

        while (i < debtors.Count && j < creditors.Count)
        {
            var amount = Math.Min(debtors[i].Remaining, creditors[j].Remaining);

            if (amount >= _epsilon)
                transfers.Add(new Transfer(debtors[i].Id, creditors[j].Id, amount));

            debtors[i].Remaining -= amount;
            creditors[j].Remaining -= amount;

            if (debtors[i].Remaining < _epsilon) i++;
            if (creditors[j].Remaining < _epsilon) j++;
        }

        return transfers;
    }

    private sealed class Node
    {
        public Guid Id { get; }
        public decimal Remaining { get; set; }

        public Node(Guid id, decimal remaining)
        {
            Id = id;
            Remaining = remaining;
        }
    }
}