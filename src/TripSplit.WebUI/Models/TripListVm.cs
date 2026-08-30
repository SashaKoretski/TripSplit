using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class TripListVm
{
    public IReadOnlyList<Trip> Trips { get; init; } = Array.Empty<Trip>();
    public Guid? CurrentTripId { get; init; }
    public CreateTripVm Create { get; init; } = new();
    public JoinTripVm Join { get; init; } = new();
}
