using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.BusinessLogic.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    // Регистрация
    public async Task<User> RegisterAsync(string name, string email, string googleId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("GoogleId is required", nameof(googleId));

        var existing = await _users.GetByGoogleIdAsync(googleId);
        if (existing is not null)
            return existing;

        var user = new User(Guid.NewGuid(), name, email, googleId);
        await _users.AddAsync(user);
        return user;
    }

    public async Task<User> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User id cannot be empty", nameof(id));

        var user = await _users.GetByIdAsync(id);
        return user ?? throw new UserNotFoundException(id);
    }

    public async Task<User?> FindByGoogleIdAsync(string googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("GoogleId is required", nameof(googleId));

        return await _users.GetByGoogleIdAsync(googleId);
    }
}
