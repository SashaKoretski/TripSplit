using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/Receipt - image несет ReceiptImage целиком (не просто флаг), WebLab#2
public class ReceiptDto
{
    public Guid Id { get; init; }
    public Guid TripId { get; init; }
    public required string Name { get; init; }
    public DateOnly Date { get; init; }
    public ReceiptImageDto? Image { get; init; }

    public static ReceiptDto FromDomain(Receipt receipt, ReceiptImageDto? image) => new()
    {
        Id = receipt.Id,
        TripId = receipt.TripId,
        Name = receipt.Name,
        Date = receipt.Date,
        Image = image
    };
}
