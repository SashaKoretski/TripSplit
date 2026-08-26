using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Repositories;

public interface IReceiptRepository
{
    Task<Receipt?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Receipt>> GetByTripAsync(Guid tripId);
    Task AddAsync(Receipt receipt);
    Task DeleteAsync(Guid id);
}
