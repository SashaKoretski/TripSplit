using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class TripDetailsVm
{
    public Trip Trip { get; init; } = default!;
    public IReadOnlyList<User> Participants { get; init; } = Array.Empty<User>();
    public bool IsSelected { get; init; }
    public InviteParticipantVm Invite { get; init; } = new();
}
