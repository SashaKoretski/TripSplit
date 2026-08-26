using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

public interface IReceiptService
{
    Task<Receipt> CreateAsync(Guid tripId, string fileUrl, DateOnly date);
    Task<Receipt> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Receipt>> GetByTripAsync(Guid tripId);
    Task DeleteAsync(Guid id);
}
