using Moq;
using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using TripSplit.DataAccess.Tests.TestDoubles;
using Xunit;

namespace TripSplit.DataAccess.Tests.Repositories;

public sealed class TripRepositoryUnitTests
{
    private static Trip NewTrip(TripStatus status = TripStatus.Active) =>
        new(Guid.NewGuid(), "Trip", "RUB", DateTime.UtcNow) { Status = status };

    private static Dictionary<string, object?> RowOf(Trip t) => new()
    {
        ["id"] = t.Id,
        ["name"] = t.Name,
        ["status"] = t.Status == TripStatus.Active ? "active" : "finished",
        ["currency"] = t.Currency,
        ["created_at"] = t.CreatedAt,
    };

    [Fact]
    public async Task GetByIdAsync_RowExists_ReturnsMappedTripWithNoParticipants()
    {
        var trip = NewTrip();
        var conn = new FakeDbConnection(
            FakeCommandScript.Reader(RowOf(trip)),
            FakeCommandScript.EmptyReader());
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(trip.Id);

        Assert.Equal(trip.Id, result!.Id);
        Assert.Empty(result.ParticipantIds);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyId_ThrowsArgumentExceptionWithoutOpeningConnection()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new TripRepository(factory.Object);

        var act = () => repo.GetByIdAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatchingTrip_ReturnsNull()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_MultipleTrips_AttachesEachTripsOwnParticipants()
    {
        var a = NewTrip();
        var b = NewTrip(TripStatus.Finished);
        var userA = Guid.NewGuid();
        var conn = new FakeDbConnection(
            FakeCommandScript.Reader(RowOf(a), RowOf(b)),
            FakeCommandScript.Reader(new Dictionary<string, object?> { ["trip_id"] = a.Id, ["user_id"] = userA }));
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(userA, result.Single(t => t.Id == a.Id).ParticipantIds);
        Assert.Empty(result.Single(t => t.Id == b.Id).ParticipantIds);
    }

    [Fact]
    public async Task GetAllAsync_NoTrips_ReturnsEmptyListWithoutLoadingParticipants()
    {
        var conn = new FakeDbConnection(FakeCommandScript.EmptyReader());
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetAllAsync();

        Assert.Empty(result);
        Assert.Single(conn.ExecutedCommands);
    }

    [Fact]
    public async Task GetByUserAsync_EmptyUserId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new TripRepository(factory.Object);

        var act = () => repo.GetByUserAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetByUserAsync_UserHasTrips_ReturnsThem()
    {
        var trip = NewTrip();
        var conn = new FakeDbConnection(
            FakeCommandScript.Reader(RowOf(trip)),
            FakeCommandScript.EmptyReader());
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        var result = await repo.GetByUserAsync(Guid.NewGuid());

        Assert.Single(result);
    }

    [Fact]
    public async Task AddAsync_Valid_InsertsTripAndReplacesParticipants()
    {
        var trip = NewTrip();
        trip.ParticipantIds.Add(Guid.NewGuid());
        var conn = new FakeDbConnection(
            FakeCommandScript.NonQuery(1),
            FakeCommandScript.NonQuery(0),
            FakeCommandScript.NonQuery(1));
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        await repo.AddAsync(trip);

        Assert.Contains("INSERT INTO trips", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task AddAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new TripRepository(factory.Object);

        var act = () => repo.AddAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task UpdateAsync_RowAffected_ReplacesParticipantsAndCommits()
    {
        var trip = NewTrip(TripStatus.Finished);
        var conn = new FakeDbConnection(
            FakeCommandScript.NonQuery(1),
            FakeCommandScript.NonQuery(0));
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        await repo.UpdateAsync(trip);

        Assert.Contains("UPDATE trips", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task UpdateAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var trip = NewTrip();
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        var act = () => repo.UpdateAsync(trip);

        await Assert.ThrowsAsync<EntityNotFoundException>(act);
    }

    [Fact]
    public async Task UpdateAsync_Null_ThrowsArgumentNullException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new TripRepository(factory.Object);

        var act = () => repo.UpdateAsync(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task DeleteAsync_RowAffected_CompletesSuccessfully()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(1));
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        await repo.DeleteAsync(Guid.NewGuid());

        Assert.Contains("DELETE FROM trips", conn.ExecutedCommands[0].CommandText);
    }

    [Fact]
    public async Task DeleteAsync_NoRowAffected_ThrowsEntityNotFoundException()
    {
        var conn = new FakeDbConnection(FakeCommandScript.NonQuery(0));
        var repo = new TripRepository(new FakeDbConnectionFactory(conn));

        var act = () => repo.DeleteAsync(Guid.NewGuid());

        await Assert.ThrowsAsync<EntityNotFoundException>(act);
    }

    [Fact]
    public async Task DeleteAsync_EmptyId_ThrowsArgumentException()
    {
        var factory = new Mock<IDbConnectionFactory>(MockBehavior.Strict);
        var repo = new TripRepository(factory.Object);

        var act = () => repo.DeleteAsync(Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }
}
