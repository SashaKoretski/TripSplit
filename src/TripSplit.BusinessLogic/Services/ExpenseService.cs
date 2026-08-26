using TripSplit.BusinessLogic.Interfaces.Repositories;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.BusinessLogic.Models.Exceptions;

namespace TripSplit.BusinessLogic.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenses;
    private readonly ITripRepository _trips;
    private readonly IReceiptRepository _receipts;

    public ExpenseService(
        IExpenseRepository expenses,
        ITripRepository trips,
        IReceiptRepository receipts)
    {
        _expenses = expenses ?? throw new ArgumentNullException(nameof(expenses));
        _trips = trips ?? throw new ArgumentNullException(nameof(trips));
        _receipts = receipts ?? throw new ArgumentNullException(nameof(receipts));
    }

    // Добавляет трату в активную поездку с проверкой участников и чека
    public async Task<Expense> AddAsync(
        Guid tripId,
        Guid payerId,
        string name,
        ExpenseType type,
        decimal value,
        decimal discount,
        IEnumerable<Guid> consumerIds,
        Guid? receiptId = null)
    {
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));

        var trip = await _trips.GetByIdAsync(tripId)
            ?? throw new TripNotFoundException(tripId);
        if (trip.Status == TripStatus.Finished)
            throw new TripAlreadyFinishedException(tripId);

        var consumers = consumerIds?.ToList()
            ?? throw new InvalidExpenseException("Consumers list is required");

        EnsureParticipants(trip, payerId, consumers);
        await EnsureReceiptBelongsToTripAsync(receiptId, tripId);

        var expense = new Expense(
            Guid.NewGuid(), tripId, payerId, name, type, value, discount, consumers, receiptId);

        await _expenses.AddAsync(expense);
        return expense;
    }

    public async Task<Expense> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Expense id cannot be empty", nameof(id));

        var expense = await _expenses.GetByIdAsync(id);
        return expense ?? throw new ExpenseNotFoundException(id);
    }

    public async Task<IReadOnlyList<Expense>> GetByTripAsync(Guid tripId)
    {
        if (tripId == Guid.Empty)
            throw new ArgumentException("Trip id cannot be empty", nameof(tripId));

        return await _expenses.GetByTripAsync(tripId);
    }

    // Полная замена данных траты
    public async Task UpdateAsync(Expense expense)
    {
        if (expense is null)
            throw new ArgumentNullException(nameof(expense));

        var existing = await _expenses.GetByIdAsync(expense.Id)
            ?? throw new ExpenseNotFoundException(expense.Id);

        if (existing.TripId != expense.TripId)
            throw new InvalidExpenseException("Expense cannot be moved to another trip");

        var trip = await _trips.GetByIdAsync(expense.TripId)
            ?? throw new TripNotFoundException(expense.TripId);
        if (trip.Status == TripStatus.Finished)
            throw new TripAlreadyFinishedException(expense.TripId);

        EnsureParticipants(trip, expense.PayerId, expense.ConsumerIds);
        await EnsureReceiptBelongsToTripAsync(expense.ReceiptId, expense.TripId);

        await _expenses.UpdateAsync(expense);
    }

    public async Task DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Expense id cannot be empty", nameof(id));

        var expense = await _expenses.GetByIdAsync(id)
            ?? throw new ExpenseNotFoundException(id);

        var trip = await _trips.GetByIdAsync(expense.TripId)
            ?? throw new TripNotFoundException(expense.TripId);
        if (trip.Status == TripStatus.Finished)
            throw new TripAlreadyFinishedException(expense.TripId);

        await _expenses.DeleteAsync(id);
    }

    // Проверяет, что плательщик и все потребители — участники поездки
    private static void EnsureParticipants(Trip trip, Guid payerId, IReadOnlyCollection<Guid> consumerIds)
    {
        if (!trip.ParticipantIds.Contains(payerId))
            throw new InvalidExpenseException($"Payer '{payerId}' is not a participant of trip '{trip.Id}'");

        foreach (var consumerId in consumerIds)
        {
            if (!trip.ParticipantIds.Contains(consumerId))
                throw new InvalidExpenseException($"Consumer '{consumerId}' is not a participant of trip '{trip.Id}'");
        }
    }

    // Проверяет, что чек существует и относится к той же поездке
    private async Task EnsureReceiptBelongsToTripAsync(Guid? receiptId, Guid tripId)
    {
        if (receiptId is null)
            return;

        var receipt = await _receipts.GetByIdAsync(receiptId.Value)
            ?? throw new ReceiptNotFoundException(receiptId.Value);
        if (receipt.TripId != tripId)
            throw new InvalidExpenseException($"Receipt '{receiptId}' does not belong to trip '{tripId}'");
    }
}
