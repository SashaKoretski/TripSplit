using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/Trip в openapi.yaml
public class TripDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }

    public static TripDto FromDomain(Trip trip) => new()
    {
        Id = trip.Id,
        Name = trip.Name,
        Currency = trip.Currency,
        Status = trip.Status == TripStatus.Active ? "active" : "finished",
        CreatedAt = trip.CreatedAt
    };
}
