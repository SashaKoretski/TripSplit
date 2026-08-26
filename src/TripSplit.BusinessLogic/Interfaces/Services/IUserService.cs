using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

public interface IUserService
{
    // Регистрирует нового пользователя или возвращает существующего по GoogleId
    Task<User> RegisterAsync(string name, string email, string googleId);
    Task<User> GetByIdAsync(Guid id);
    Task<User?> FindByGoogleIdAsync(string googleId);
}
