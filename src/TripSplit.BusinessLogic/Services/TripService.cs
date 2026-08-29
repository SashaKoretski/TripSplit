using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.BusinessLogic.Services;

public class TripService : ITripService
{
    private readonly ITripRepository _trips;
    private readonly IUserRepository _users;

    public TripService(ITripRepository trips, IUserRepository users)
    {
        _trips = trips ?? throw new ArgumentNullException(nameof(trips));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    // Создает активную поездку и добавляет организатора первым участником
    public async Task<Trip> CreateAsync(string name, string currency, Guid organizerId)
    {
        if (organizerId == Guid.Empty)
            throw new ArgumentException("Organizer id cannot be empty", nameof(organizerId));

        var organizer = await _users.GetByIdAsync(organizerId)
            ?? throw new UserNotFoundException(organizerId);

        var trip = new Trip(Guid.NewGuid(), name, currency, DateTime.UtcNow);
        trip.ParticipantIds.Add(organizer.Id);

        await _trips.AddAsync(trip);
        return trip;
    }

    public async Task<Trip> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(id));

        var trip = await _trips.GetByIdAsync(id);
        return trip ?? throw new TripNotFoundException(id);
    }

    public async Task<IReadOnlyList<Trip>> GetByUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty", nameof(userId));

        return await _trips.GetByUserAsync(userId);
    }

    // Добавляет участника в поездку
    public async Task AddParticipantAsync(Guid tripId, Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty", nameof(userId));

        var trip = await GetByIdAsync(tripId);
        if (trip.Status == TripStatus.Finished)
            throw new TripAlreadyFinishedException(tripId);

        _ = await _users.GetByIdAsync(userId)
            ?? throw new UserNotFoundException(userId);

        if (trip.ParticipantIds.Contains(userId))
            return;

        trip.ParticipantIds.Add(userId);
        await _trips.UpdateAsync(trip);
    }

    public async Task FinishAsync(Guid tripId)
    {
        var trip = await GetByIdAsync(tripId);
        if (trip.Status == TripStatus.Finished)
            throw new TripAlreadyFinishedException(tripId);

        trip.Status = TripStatus.Finished;
        await _trips.UpdateAsync(trip);
    }
}
