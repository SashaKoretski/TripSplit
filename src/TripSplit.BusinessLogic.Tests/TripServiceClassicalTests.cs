using Microsoft.VisualStudio.TestTools.UnitTesting;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;
using TripSplit.BusinessLogic.Tests.TestSupport;

namespace TripSplit.BusinessLogic.Tests;

/// <summary>
/// Классический (Detroit-school) вариант части сценариев TripServiceTests.cs (Moq/Лондонский
/// стиль) — для прямого сравнения (ЛР1 Т3). Здесь обе зависимости — реальные in-memory fakes;
/// проверяется итоговое состояние, а не вызовы.
/// </summary>
[TestClass]
public class TripServiceClassicalTests
{
    private InMemoryTripRepository _trips = null!;
    private InMemoryUserRepository _users = null!;
    private TripService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _trips = new InMemoryTripRepository();
        _users = new InMemoryUserRepository();
        _sut = new TripService(_trips, _users);
    }

    // Классический аналог TripServiceTests.CreateAsync_ValidInput_CreatesActiveTripWithOrganizerAsParticipant.
    [TestMethod]
    public async Task CreateAsync_ValidInput_CreatesActiveTripWithOrganizerAsParticipant()
    {
        var organizer = ObjectMother.Sasha();
        await _users.AddAsync(organizer);

        var trip = await _sut.CreateAsync("Bali 2026", "USD", organizer.Id);

        var persisted = await _trips.GetByIdAsync(trip.Id);
        Assert.IsNotNull(persisted);
        Assert.AreEqual(TripStatus.Active, persisted!.Status);
        CollectionAssert.Contains(persisted.ParticipantIds, organizer.Id);
    }

    // Классический аналог TripServiceTests.AddParticipantAsync_NewParticipant_AddedAndPersisted.
    [TestMethod]
    public async Task AddParticipantAsync_NewParticipant_AddedAndPersisted()
    {
        var organizer = ObjectMother.Sasha();
        var newcomer = ObjectMother.Masha();
        await _users.AddAsync(organizer);
        await _users.AddAsync(newcomer);
        var trip = await _sut.CreateAsync("Trip", "RUB", organizer.Id);

        await _sut.AddParticipantAsync(trip.Id, newcomer.Id);

        var persisted = await _trips.GetByIdAsync(trip.Id);
        CollectionAssert.Contains(persisted!.ParticipantIds, newcomer.Id);
    }

    // Классический аналог TripServiceTests.FinishAsync_AlreadyFinished_Throws.
    [TestMethod]
    public async Task FinishAsync_AlreadyFinished_Throws()
    {
        var organizer = ObjectMother.Sasha();
        await _users.AddAsync(organizer);
        var trip = await _sut.CreateAsync("Trip", "RUB", organizer.Id);
        await _sut.FinishAsync(trip.Id);

        await Assert.ThrowsExactlyAsync<TripAlreadyFinishedException>(() => _sut.FinishAsync(trip.Id));
    }
}
