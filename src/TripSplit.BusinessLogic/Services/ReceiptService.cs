using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.BusinessLogic.Services;

public class ReceiptService : IReceiptService
{
    private readonly IReceiptRepository _receipts;
    private readonly ITripRepository _trips;
    private readonly IReceiptImageService _images;

    public ReceiptService(IReceiptRepository receipts, ITripRepository trips, IReceiptImageService images)
    {
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
        _trips = trips ?? throw new ArgumentNullException(nameof(trips));
        _images = images ?? throw new ArgumentNullException(nameof(images));
    }

    // Регистрирует чек в активной поездке
    public async Task<Receipt> CreateAsync(Guid tripId, string name, DateOnly date)
    {
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));

        var trip = await _trips.GetByIdAsync(tripId)
            ?? throw new TripNotFoundException(tripId);
        if (trip.Status == TripStatus.Finished)
            throw new TripAlreadyFinishedException(tripId);

        var receipt = new Receipt(Guid.NewGuid(), tripId, name, date);
        await _receipts.AddAsync(receipt);
        return receipt;
    }

    public async Task<Receipt> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(id));

        var receipt = await _receipts.GetByIdAsync(id);
        return receipt ?? throw new ReceiptNotFoundException(id);
    }

    public async Task<IReadOnlyList<Receipt>> GetByTripAsync(Guid tripId)
    {
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));

        return await _receipts.GetByTripAsync(tripId);
    }

    public async Task DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Receipt id cannot be empty", nameof(id));

        var receipt = await _receipts.GetByIdAsync(id)
            ?? throw new ReceiptNotFoundException(id);

        await _images.DeleteByReceiptAsync(receipt.Id);
        await _receipts.DeleteAsync(receipt.Id);
    }
}
