using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/Expense
public class ExpenseDto
{
    public Guid Id { get; init; }
    public Guid TripId { get; init; }
    public Guid? ReceiptId { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public decimal Value { get; init; }
    public decimal Discount { get; init; }
    public decimal EffectiveAmount { get; init; }
    public Guid PayerId { get; init; }
    public required IReadOnlyList<Guid> ConsumerIds { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static ExpenseDto FromDomain(Expense e) => new()
    {
        Id = e.Id,
        TripId = e.TripId,
        ReceiptId = e.ReceiptId,
        Name = e.Name,
        Type = e.Type.ToString().ToLowerInvariant(),
        Value = e.Value,
        Discount = e.Discount,
        EffectiveAmount = e.EffectiveAmount,
        PayerId = e.PayerId,
        ConsumerIds = e.ConsumerIds,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
