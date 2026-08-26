using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Repositories;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Expense>> GetByTripAsync(Guid tripId);
    Task AddAsync(Expense expense);
    Task UpdateAsync(Expense expense);
    Task DeleteAsync(Guid id);
}
