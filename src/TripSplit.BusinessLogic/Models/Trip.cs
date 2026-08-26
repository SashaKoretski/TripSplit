namespace TripSplit.BusinessLogic.Models;

public class Trip
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public TripStatus Status { get; set; }
    public string Currency { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<Guid> ParticipantIds { get; init; } = new();

    public Trip(Guid id, string name, string currency, DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trip name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be ISO 4217 (3 letters)", nameof(currency));

        Id = id;
        Name = name;
        Currency = currency.ToUpperInvariant();
        CreatedAt = createdAt;
        Status = TripStatus.Active;
    }
}
