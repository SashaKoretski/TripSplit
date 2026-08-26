using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class TripServiceTests
{
    private Mock<ITripRepository> _trips = null!;
    private Mock<IUserRepository> _users = null!;
    private TripService _sut = null!;

    private static User MakeUser(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "a", "a@e.com", "g-" + Guid.NewGuid());

    private static Trip MakeTrip(Guid? id = null, TripStatus status = TripStatus.Active)
    {
        var trip = new Trip(id ?? Guid.NewGuid(), "Test Trip", "RUB", DateTime.UtcNow);
        trip.Status = status;
        return trip;
    }

    [TestInitialize]
    public void Setup()
    {
        _trips = new Mock<ITripRepository>();
        _users = new Mock<IUserRepository>();
        _sut = new TripService(_trips.Object, _users.Object);
    }

    [TestMethod]
    public async Task CreateAsync_ValidInput_CreatesActiveTripWithOrganizerAsParticipant()
    {
        var organizer = MakeUser();
        _users.Setup(r => r.GetByIdAsync(organizer.Id)).ReturnsAsync(organizer);

        var trip = await _sut.CreateAsync("Bali 2026", "USD", organizer.Id);

        Assert.IsNotNull(trip);
        Assert.AreNotEqual(Guid.Empty, trip.Id);
        Assert.AreEqual("Bali 2026", trip.Name);
        Assert.AreEqual("USD", trip.Currency);
        Assert.AreEqual(TripStatus.Active, trip.Status);
        CollectionAssert.Contains(trip.ParticipantIds, organizer.Id);
        _trips.Verify(r => r.AddAsync(It.Is<Trip>(t => t.Id == trip.Id)), Times.Once);
    }

    [TestMethod]
    public async Task CreateAsync_OrganizerNotFound_ThrowsUserNotFoundException()
    {
        var organizerId = Guid.NewGuid();
        _users.Setup(r => r.GetByIdAsync(organizerId)).ReturnsAsync((User?)null);

        await Assert.ThrowsExceptionAsync<UserNotFoundException>(
            () => _sut.CreateAsync("Trip", "RUB", organizerId));

        _trips.Verify(r => r.AddAsync(It.IsAny<Trip>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_EmptyOrganizerId_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.CreateAsync("Trip", "RUB", Guid.Empty));

        _users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [DataTestMethod]
    [DataRow("", "RUB")]
    [DataRow("  ", "RUB")]
    [DataRow("Trip", "")]
    [DataRow("Trip", "RU")]
    [DataRow("Trip", "RUBB")]
    public async Task CreateAsync_InvalidNameOrCurrency_ThrowsFromTripCtor(string name, string currency)
    {
        var organizer = MakeUser();
        _users.Setup(r => r.GetByIdAsync(organizer.Id)).ReturnsAsync(organizer);

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.CreateAsync(name, currency, organizer.Id));

        _trips.Verify(r => r.AddAsync(It.IsAny<Trip>()), Times.Never);
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingTrip_ReturnsTrip()
    {
        var trip = MakeTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        var result = await _sut.GetByIdAsync(trip.Id);

        Assert.AreSame(trip, result);
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFound_ThrowsTripNotFoundException()
    {
        var id = Guid.NewGuid();
        _trips.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsExceptionAsync<TripNotFoundException>(
            () => _sut.GetByIdAsync(id));
    }

    [TestMethod]
    public async Task GetByIdAsync_EmptyId_ThrowsArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.GetByIdAsync(Guid.Empty));

        _trips.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task GetByUserAsync_ExistingUser_ReturnsTrips()
    {
        var userId = Guid.NewGuid();
        var trips = new List<Trip> { MakeTrip(), MakeTrip() };
        _trips.Setup(r => r.GetByUserAsync(userId)).ReturnsAsync(trips);

        var result = await _sut.GetByUserAsync(userId);

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetByUserAsync_EmptyId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.GetByUserAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task AddParticipantAsync_NewParticipant_AddedAndPersisted()
    {
        var trip = MakeTrip();
        var user = MakeUser();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await _sut.AddParticipantAsync(trip.Id, user.Id);

        CollectionAssert.Contains(trip.ParticipantIds, user.Id);
        _trips.Verify(r => r.UpdateAsync(trip), Times.Once);
    }

    [TestMethod]
    public async Task AddParticipantAsync_AlreadyParticipant_NoUpdate()
    {
        var trip = MakeTrip();
        var user = MakeUser();
        trip.ParticipantIds.Add(user.Id);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _users.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await _sut.AddParticipantAsync(trip.Id, user.Id);

        Assert.AreEqual(1, trip.ParticipantIds.Count(id => id == user.Id));
        _trips.Verify(r => r.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }

    [TestMethod]
    public async Task AddParticipantAsync_TripFinished_Throws()
    {
        var trip = MakeTrip(status: TripStatus.Finished);
        var user = MakeUser();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<TripAlreadyFinishedException>(
            () => _sut.AddParticipantAsync(trip.Id, user.Id));

        _users.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _trips.Verify(r => r.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }

    [TestMethod]
    public async Task AddParticipantAsync_TripNotFound_Throws()
    {
        var tripId = Guid.NewGuid();
        _trips.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsExceptionAsync<TripNotFoundException>(
            () => _sut.AddParticipantAsync(tripId, Guid.NewGuid()));
    }

    [TestMethod]
    public async Task AddParticipantAsync_UserNotFound_Throws()
    {
        var trip = MakeTrip();
        var userId = Guid.NewGuid();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
        _users.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        await Assert.ThrowsExceptionAsync<UserNotFoundException>(
            () => _sut.AddParticipantAsync(trip.Id, userId));

        _trips.Verify(r => r.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }

    [TestMethod]
    public async Task AddParticipantAsync_EmptyUserId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.AddParticipantAsync(Guid.NewGuid(), Guid.Empty));

        _trips.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task FinishAsync_ActiveTrip_MarksFinishedAndPersists()
    {
        var trip = MakeTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await _sut.FinishAsync(trip.Id);

        Assert.AreEqual(TripStatus.Finished, trip.Status);
        _trips.Verify(r => r.UpdateAsync(trip), Times.Once);
    }

    [TestMethod]
    public async Task FinishAsync_AlreadyFinished_Throws()
    {
        var trip = MakeTrip(status: TripStatus.Finished);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<TripAlreadyFinishedException>(
            () => _sut.FinishAsync(trip.Id));

        _trips.Verify(r => r.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }

    [TestMethod]
    public async Task FinishAsync_TripNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _trips.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsExceptionAsync<TripNotFoundException>(
            () => _sut.FinishAsync(id));
    }
}
