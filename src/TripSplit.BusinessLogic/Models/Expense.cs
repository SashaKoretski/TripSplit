using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.BusinessLogic.Models;

public class Expense
{
    public Guid Id { get; init; }
    public Guid TripId { get; init; }
    public Guid? ReceiptId { get; init; }
    public Guid PayerId { get; init; }
    public string Name { get; init; }
    public ExpenseType Type { get; init; }
    public decimal Value { get; init; }
    public decimal Discount { get; init; }
    public List<Guid> ConsumerIds { get; init; } = new();

    // Сумма, которая реально делится между потребителями
    public decimal EffectiveAmount => Value - Discount;

    public Expense(
        Guid id,
        Guid tripId,
        Guid payerId,
        string name,
        ExpenseType type,
        decimal value,
        decimal discount,
        IEnumerable<Guid> consumerIds,
        Guid? receiptId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Expense id cannot be empty", nameof(id));
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));
        if (payerId == Guid.Empty)
            throw new ArgumentException("Payer id cannot be empty", nameof(payerId));
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidExpenseException("Expense name is required");
        if (value <= 0)
            throw new InvalidExpenseException("Expense value must be positive");
        if (discount < 0)
            throw new InvalidExpenseException("Discount cannot be negative");
        if (discount > value)
            throw new InvalidExpenseException("Discount cannot exceed value");

        var consumers = consumerIds?.ToList()
            ?? throw new InvalidExpenseException("Consumers list is required");
        if (consumers.Count == 0)
            throw new InvalidExpenseException("Expense must have at least one consumer");
        if (consumers.Any(c => c == Guid.Empty))
            throw new InvalidExpenseException("Consumer id cannot be empty");

        Id = id;
        TripId = tripId;
        ReceiptId = receiptId;
        PayerId = payerId;
        Name = name;
        Type = type;
        Value = value;
        Discount = discount;
        ConsumerIds = consumers;
    }
}
