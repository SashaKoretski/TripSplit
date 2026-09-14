using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Interfaces.Services;

public interface IUserService
{
    // Регистрирует нового пользователя, либо возвращает существующего по GoogleId или email
    Task<User> RegisterAsync(string name, string email, string googleId);
    Task<User> GetByIdAsync(Guid id);
    Task<User?> FindByGoogleIdAsync(string googleId);
    Task<User?> FindByEmailAsync(string email);
}
