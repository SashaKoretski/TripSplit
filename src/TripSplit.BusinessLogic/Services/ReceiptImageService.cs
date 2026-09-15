using System.IO;
using TripSplit.BusinessLogic.Configuration;
using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.BusinessLogic.Services;

public class ReceiptImageService : IReceiptImageService
{
    private static readonly TimeSpan DownloadUrlLifetime = TimeSpan.FromMinutes(15);

    private readonly IReceiptImageRepository _images;
    private readonly IReceiptRepository _receipts;
    private readonly ITripRepository _trips;
    private readonly IFileStorageService _storage;
    private readonly ReceiptImageOptions _options;

    public ReceiptImageService(
        IReceiptImageRepository images, IReceiptRepository receipts, ITripRepository trips,
        IFileStorageService storage, ReceiptImageOptions options)
    {
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        _trips = trips ?? throw new ArgumentNullException(nameof(trips));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    // Загружает файл в S3 и (пере)привязывает его к чеку; предыдущее изображение, если было, удаляется
    public async Task<ReceiptImage> UploadAsync(
        Guid receiptId, Stream content, string originalFileName, string contentType, long fileSizeBytes)
    {
        if (receiptId == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(receiptId));
        if (fileSizeBytes <= 0)
            throw new InvalidReceiptImageException("Файл пуст");
        if (fileSizeBytes > _options.MaxFileSizeBytes)
            throw new InvalidReceiptImageException($"Файл превышает лимит в {_options.MaxFileSizeBytes / (1024 * 1024)} МБ");
        if (string.IsNullOrWhiteSpace(contentType) || !_options.AllowedContentTypes.Contains(contentType))
            throw new InvalidReceiptImageException("Недопустимый тип файла");

        var receipt = await _receipts.GetByIdAsync(receiptId)
            ?? throw new ReceiptNotFoundException(receiptId);
        var trip = await _trips.GetByIdAsync(receipt.TripId)
            ?? throw new TripNotFoundException(receipt.TripId);
        if (trip.Status == TripStatus.Finished)
            throw new TripAlreadyFinishedException(trip.Id);

        await DeleteByReceiptAsync(receiptId);

        var extension = Path.GetExtension(originalFileName);
        var key = $"receipts/{receipt.TripId}/{receiptId}/{Guid.NewGuid()}{extension}";
        await _storage.UploadAsync(key, content, contentType);

        var image = new ReceiptImage(
            Guid.NewGuid(), receiptId, key, contentType, fileSizeBytes, originalFileName, DateTime.UtcNow);
        await _images.AddAsync(image);
        return image;
    }

    public async Task<ReceiptImage?> GetByReceiptAsync(Guid receiptId)
    {
        if (receiptId == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(receiptId));

        return await _images.GetByReceiptAsync(receiptId);
    }

    public async Task<string> GetDownloadUrlAsync(Guid receiptId)
    {
        var image = await GetByReceiptAsync(receiptId)
            ?? throw new ReceiptImageNotFoundException(receiptId);

        return await _storage.GetPresignedUrlAsync(image.StorageKey, DownloadUrlLifetime);
    }

    // Удаляет файл из S3 и запись в БД; не бросает исключение, если изображения нет
    public async Task DeleteByReceiptAsync(Guid receiptId)
    {
        if (receiptId == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(receiptId));

        var image = await _images.GetByReceiptAsync(receiptId);
        if (image is null) return;

        await _storage.DeleteAsync(image.StorageKey);
        await _images.DeleteAsync(image.Id);
    }
}
