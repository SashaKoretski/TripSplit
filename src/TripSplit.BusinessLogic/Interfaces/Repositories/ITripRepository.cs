using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Repositories;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Trip>> GetAllAsync();
    Task<IReadOnlyList<Trip>> GetByUserAsync(Guid userId);
    Task AddAsync(Trip trip);
    Task UpdateAsync(Trip trip);
    Task DeleteAsync(Guid id);
}
