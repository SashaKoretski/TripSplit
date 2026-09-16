using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Repositories;

public interface IReceiptImageRepository
{
    Task<ReceiptImage?> GetByReceiptAsync(Guid receiptId);
    Task AddAsync(ReceiptImage image);
    Task DeleteAsync(Guid id);
}
