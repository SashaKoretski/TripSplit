using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.TestSupport;

/// <summary>
/// Классические (state-based) тестовые дублеры — fakes, а не mock/stub из Moq. Хранят
/// реальное состояние в памяти и используются для "классического" варианта тестов (Т3),
/// в противовес "Лондонскому" варианту на Moq (*ServiceTests.cs), который проверяет
/// взаимодействия (Verify), а не состояние.
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _byId = new();

    public int AddCallCount { get; private set; }

    public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(_byId.GetValueOrDefault(id));

    public Task<User?> GetByGoogleIdAsync(string googleId) =>
        Task.FromResult(_byId.Values.FirstOrDefault(u => u.GoogleId == googleId));

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(_byId.Values.FirstOrDefault(u => u.Email == email));

    public Task AddAsync(User user)
    {
        _byId.Add(user.Id, user);
        AddCallCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        _byId[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _byId.Remove(id);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryTripRepository : ITripRepository
{
    private readonly Dictionary<Guid, Trip> _byId = new();

    public Task<Trip?> GetByIdAsync(Guid id) => Task.FromResult(_byId.GetValueOrDefault(id));

    public Task<IReadOnlyList<Trip>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<Trip>>(_byId.Values.ToList());

    public Task<IReadOnlyList<Trip>> GetByUserAsync(Guid userId) =>
        Task.FromResult<IReadOnlyList<Trip>>(_byId.Values.Where(t => t.ParticipantIds.Contains(userId)).ToList());

    public Task AddAsync(Trip trip)
    {
        _byId.Add(trip.Id, trip);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Trip trip)
    {
        _byId[trip.Id] = trip;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _byId.Remove(id);
        return Task.CompletedTask;
    }
}
