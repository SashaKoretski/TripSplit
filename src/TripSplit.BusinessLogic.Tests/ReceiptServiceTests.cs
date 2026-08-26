using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class ReceiptServiceTests
{
    private Mock<IReceiptRepository> _receipts = null!;
    private Mock<ITripRepository> _trips = null!;
    private ReceiptService _sut = null!;

    private static Trip MakeTrip(Guid? id = null, TripStatus status = TripStatus.Active)
    {
        var trip = new Trip(id ?? Guid.NewGuid(), "Test", "RUB", DateTime.UtcNow);
        trip.Status = status;
        return trip;
    }

    private static Receipt MakeReceipt(Guid? id = null, Guid? tripId = null) =>
        new(id ?? Guid.NewGuid(), tripId ?? Guid.NewGuid(), "https://s3/x.jpg", new DateOnly(2026, 8, 25));

    [TestInitialize]
    public void Setup()
    {
        _receipts = new Mock<IReceiptRepository>();
        _trips = new Mock<ITripRepository>();
        _sut = new ReceiptService(_receipts.Object, _trips.Object);
    }

    [TestMethod]
    public async Task CreateAsync_ActiveTrip_CreatesAndPersists()
    {
        var trip = MakeTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        var receipt = await _sut.CreateAsync(trip.Id, "https://s3/x.jpg", new DateOnly(2026, 8, 25));

        Assert.IsNotNull(receipt);
        Assert.AreNotEqual(Guid.Empty, receipt.Id);
        Assert.AreEqual(trip.Id, receipt.TripId);
        Assert.AreEqual("https://s3/x.jpg", receipt.FileUrl);
        _receipts.Verify(r => r.AddAsync(It.Is<Receipt>(x => x.Id == receipt.Id)), Times.Once);
    }

    [TestMethod]
    public async Task CreateAsync_TripNotFound_Throws()
    {
        var tripId = Guid.NewGuid();
        _trips.Setup(r => r.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsExceptionAsync<TripNotFoundException>(
            () => _sut.CreateAsync(tripId, "https://s3/x.jpg", new DateOnly(2026, 8, 25)));

        _receipts.Verify(r => r.AddAsync(It.IsAny<Receipt>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_TripFinished_Throws()
    {
        var trip = MakeTrip(status: TripStatus.Finished);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<TripAlreadyFinishedException>(
            () => _sut.CreateAsync(trip.Id, "https://s3/x.jpg", new DateOnly(2026, 8, 25)));

        _receipts.Verify(r => r.AddAsync(It.IsAny<Receipt>()), Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_EmptyTripId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.CreateAsync(Guid.Empty, "https://s3/x.jpg", new DateOnly(2026, 8, 25)));

        _trips.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task CreateAsync_InvalidFileUrl_Throws(string fileUrl)
    {
        var trip = MakeTrip();
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.CreateAsync(trip.Id, fileUrl, new DateOnly(2026, 8, 25)));

        _receipts.Verify(r => r.AddAsync(It.IsAny<Receipt>()), Times.Never);
    }

    [TestMethod]
    public async Task GetByIdAsync_Existing_ReturnsReceipt()
    {
        var receipt = MakeReceipt();
        _receipts.Setup(r => r.GetByIdAsync(receipt.Id)).ReturnsAsync(receipt);

        var result = await _sut.GetByIdAsync(receipt.Id);

        Assert.AreSame(receipt, result);
    }

    [TestMethod]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        var id = Guid.NewGuid();
        _receipts.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Receipt?)null);

        await Assert.ThrowsExceptionAsync<ReceiptNotFoundException>(
            () => _sut.GetByIdAsync(id));
    }

    [TestMethod]
    public async Task GetByIdAsync_EmptyId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.GetByIdAsync(Guid.Empty));

        _receipts.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task GetByTripAsync_ReturnsList()
    {
        var tripId = Guid.NewGuid();
        var list = new List<Receipt> { MakeReceipt(tripId: tripId), MakeReceipt(tripId: tripId) };
        _receipts.Setup(r => r.GetByTripAsync(tripId)).ReturnsAsync(list);

        var result = await _sut.GetByTripAsync(tripId);

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetByTripAsync_EmptyId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.GetByTripAsync(Guid.Empty));

        _receipts.Verify(r => r.GetByTripAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteAsync_Existing_Deletes()
    {
        var receipt = MakeReceipt();
        _receipts.Setup(r => r.GetByIdAsync(receipt.Id)).ReturnsAsync(receipt);

        await _sut.DeleteAsync(receipt.Id);

        _receipts.Verify(r => r.DeleteAsync(receipt.Id), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAsync_NotFound_Throws()
    {
        var id = Guid.NewGuid();
        _receipts.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Receipt?)null);

        await Assert.ThrowsExceptionAsync<ReceiptNotFoundException>(
            () => _sut.DeleteAsync(id));

        _receipts.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteAsync_EmptyId_Throws()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _sut.DeleteAsync(Guid.Empty));

        _receipts.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }
}
