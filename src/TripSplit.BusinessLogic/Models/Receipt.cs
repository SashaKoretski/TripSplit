namespace TripSplit.BusinessLogic.Models;

public class Receipt
{
    public Guid Id { get; init; }
    public Guid TripId { get; init; }
    public string FileUrl { get; init; }
    public DateOnly Date { get; init; }

    public Receipt(Guid id, Guid tripId, string fileUrl, DateOnly date)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(id));
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("File URL is required", nameof(fileUrl));

        Id = id;
        TripId = tripId;
        FileUrl = fileUrl;
        Date = date;
    }
}
