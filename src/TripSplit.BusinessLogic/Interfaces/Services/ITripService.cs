using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

public interface ITripService
{
    Task<Trip> CreateAsync(string name, string currency, Guid organizerId);
    Task<Trip> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Trip>> GetByUserAsync(Guid userId);
    Task AddParticipantAsync(Guid tripId, Guid userId);
    Task FinishAsync(Guid tripId);
}
