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

        var existingByGoogleId = await _users.GetByGoogleIdAsync(googleId);
        if (existingByGoogleId is not null)
            return existingByGoogleId;

        // почта уже занята другим аккаунтом (иначе упадем на unique-constraint) — входим в него под сохраненным именем
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingByEmail = await _users.GetByEmailAsync(normalizedEmail);
        if (existingByEmail is not null)
            return existingByEmail;

        var user = new User(Guid.NewGuid(), name, normalizedEmail, googleId);
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

    public async Task<User?> FindByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        return await _users.GetByEmailAsync(email.Trim().ToLowerInvariant());
    }
}
