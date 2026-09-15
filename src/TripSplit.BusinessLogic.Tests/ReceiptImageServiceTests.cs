using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.BusinessLogic.Services;

namespace TripSplit.BusinessLogic.Tests;

[TestClass]
public class ReceiptImageServiceTests
{
    private Mock<IReceiptImageRepository> _images = null!;
    private Mock<IReceiptRepository> _receipts = null!;
    private Mock<ITripRepository> _trips = null!;
    private Mock<IFileStorageService> _storage = null!;
    private ReceiptImageService _sut = null!;

    private static Trip MakeTrip(Guid? id = null, TripStatus status = TripStatus.Active)
    {
        var trip = new Trip(id ?? Guid.NewGuid(), "Test", "RUB", DateTime.UtcNow);
        trip.Status = status;
        return trip;
    }

    private static Receipt MakeReceipt(Guid? id = null, Guid? tripId = null) =>
        new(id ?? Guid.NewGuid(), tripId ?? Guid.NewGuid(), "Кафе", new DateOnly(2026, 8, 25));

    private static ReceiptImage MakeImage(Guid? id = null, Guid? receiptId = null) => new(
        id ?? Guid.NewGuid(), receiptId ?? Guid.NewGuid(), "receipts/x/y/z.jpg",
        "image/jpeg", 1024, "check.jpg", DateTime.UtcNow);

    [TestInitialize]
    public void Setup()
    {
        _images = new Mock<IReceiptImageRepository>();
        _receipts = new Mock<IReceiptRepository>();
        _trips = new Mock<ITripRepository>();
        _storage = new Mock<IFileStorageService>();
        _sut = new ReceiptImageService(_images.Object, _receipts.Object, _trips.Object, _storage.Object, new ReceiptImageOptions());
    }

    private void SetupActiveReceipt(Receipt receipt, Trip trip)
    {
        _receipts.Setup(r => r.GetByIdAsync(receipt.Id)).ReturnsAsync(receipt);
        _trips.Setup(r => r.GetByIdAsync(trip.Id)).ReturnsAsync(trip);
    }

    [TestMethod]
    public async Task UploadAsync_Valid_UploadsAndPersists()
    {
        var trip = MakeTrip();
        var receipt = MakeReceipt(tripId: trip.Id);
        SetupActiveReceipt(receipt, trip);
        _images.Setup(i => i.GetByReceiptAsync(receipt.Id)).ReturnsAsync((ReceiptImage?)null);
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });

        var image = await _sut.UploadAsync(receipt.Id, content, "check.jpg", "image/jpeg", 3);

        Assert.AreEqual(receipt.Id, image.ReceiptId);
        Assert.AreEqual("image/jpeg", image.ContentType);
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), content, "image/jpeg", It.IsAny<CancellationToken>()), Times.Once);
        _images.Verify(i => i.AddAsync(It.Is<ReceiptImage>(x => x.ReceiptId == receipt.Id)), Times.Once);
    }

    [TestMethod]
    public async Task UploadAsync_ExistingImage_ReplacesOldOne()
    {
        var trip = MakeTrip();
        var receipt = MakeReceipt(tripId: trip.Id);
        SetupActiveReceipt(receipt, trip);
        var old = MakeImage(receiptId: receipt.Id);
        _images.SetupSequence(i => i.GetByReceiptAsync(receipt.Id))
            .ReturnsAsync(old)
            .ReturnsAsync(old);
        using var content = new MemoryStream(new byte[] { 1 });

        await _sut.UploadAsync(receipt.Id, content, "new.jpg", "image/jpeg", 1);

        _storage.Verify(s => s.DeleteAsync(old.StorageKey, It.IsAny<CancellationToken>()), Times.Once);
        _images.Verify(i => i.DeleteAsync(old.Id), Times.Once);
        _images.Verify(i => i.AddAsync(It.IsAny<ReceiptImage>()), Times.Once);
    }

    [TestMethod]
    public async Task UploadAsync_TooLarge_Throws()
    {
        var options = new ReceiptImageOptions { MaxFileSizeBytes = 10 };
        _sut = new ReceiptImageService(_images.Object, _receipts.Object, _trips.Object, _storage.Object, options);
        using var content = new MemoryStream(new byte[20]);

        await Assert.ThrowsExceptionAsync<InvalidReceiptImageException>(
            () => _sut.UploadAsync(Guid.NewGuid(), content, "a.jpg", "image/jpeg", 20));

        _receipts.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task UploadAsync_UnsupportedContentType_Throws()
    {
        using var content = new MemoryStream(new byte[1]);

        await Assert.ThrowsExceptionAsync<InvalidReceiptImageException>(
            () => _sut.UploadAsync(Guid.NewGuid(), content, "a.exe", "application/x-msdownload", 1));
    }

    [TestMethod]
    public async Task UploadAsync_ReceiptNotFound_Throws()
    {
        var receiptId = Guid.NewGuid();
        _receipts.Setup(r => r.GetByIdAsync(receiptId)).ReturnsAsync((Receipt?)null);
        using var content = new MemoryStream(new byte[1]);

        await Assert.ThrowsExceptionAsync<ReceiptNotFoundException>(
            () => _sut.UploadAsync(receiptId, content, "a.jpg", "image/jpeg", 1));
    }

    [TestMethod]
    public async Task UploadAsync_TripFinished_Throws()
    {
        var trip = MakeTrip(status: TripStatus.Finished);
        var receipt = MakeReceipt(tripId: trip.Id);
        SetupActiveReceipt(receipt, trip);
        using var content = new MemoryStream(new byte[1]);

        await Assert.ThrowsExceptionAsync<TripAlreadyFinishedException>(
            () => _sut.UploadAsync(receipt.Id, content, "a.jpg", "image/jpeg", 1));

        _images.Verify(i => i.AddAsync(It.IsAny<ReceiptImage>()), Times.Never);
    }

    [TestMethod]
    public async Task GetDownloadUrlAsync_Existing_ReturnsPresignedUrl()
    {
        var image = MakeImage();
        _images.Setup(i => i.GetByReceiptAsync(image.ReceiptId)).ReturnsAsync(image);
        _storage.Setup(s => s.GetPresignedUrlAsync(image.StorageKey, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://minio/presigned");

        var url = await _sut.GetDownloadUrlAsync(image.ReceiptId);

        Assert.AreEqual("https://minio/presigned", url);
    }

    [TestMethod]
    public async Task GetDownloadUrlAsync_NoImage_Throws()
    {
        var receiptId = Guid.NewGuid();
        _images.Setup(i => i.GetByReceiptAsync(receiptId)).ReturnsAsync((ReceiptImage?)null);

        await Assert.ThrowsExceptionAsync<ReceiptImageNotFoundException>(
            () => _sut.GetDownloadUrlAsync(receiptId));
    }

    [TestMethod]
    public async Task DeleteByReceiptAsync_Existing_DeletesFromStorageAndRepository()
    {
        var image = MakeImage();
        _images.Setup(i => i.GetByReceiptAsync(image.ReceiptId)).ReturnsAsync(image);

        await _sut.DeleteByReceiptAsync(image.ReceiptId);

        _storage.Verify(s => s.DeleteAsync(image.StorageKey, It.IsAny<CancellationToken>()), Times.Once);
        _images.Verify(i => i.DeleteAsync(image.Id), Times.Once);
    }

    [TestMethod]
    public async Task DeleteByReceiptAsync_NoImage_DoesNothing()
    {
        var receiptId = Guid.NewGuid();
        _images.Setup(i => i.GetByReceiptAsync(receiptId)).ReturnsAsync((ReceiptImage?)null);

        await _sut.DeleteByReceiptAsync(receiptId);

        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _images.Verify(i => i.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
