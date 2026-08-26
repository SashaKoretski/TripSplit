using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

public interface IExpenseService
{
    // Добавляет трату в активную поездку с проверкой участников
    Task<Expense> AddAsync(
        Guid tripId,
        Guid payerId,
        string name,
        ExpenseType type,
        decimal value,
        decimal discount,
        IEnumerable<Guid> consumerIds,
        Guid? receiptId = null);

    Task<Expense> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Expense>> GetByTripAsync(Guid tripId);
    Task UpdateAsync(Expense expense);
    Task DeleteAsync(Guid id);
}
