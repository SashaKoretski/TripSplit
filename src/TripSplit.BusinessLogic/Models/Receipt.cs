namespace TripSplit.BusinessLogic.Models;

public class Receipt
{
    public Guid Id { get; init; }
    public Guid TripId { get; init; }
    public string Name { get; init; }
    public DateOnly Date { get; init; }

    public Receipt(Guid id, Guid tripId, string name, DateOnly date)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(id));
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Receipt name is required", nameof(name));

        Id = id;
        TripId = tripId;
        Name = name;
        Date = date;
    }
}
