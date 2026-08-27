using TripSplit.BusinessLogic.Models;
using TripSplit.DataAccess.Infrastructure.Exceptions;
using TripSplit.DataAccess.Repositories;
using Xunit;

namespace TripSplit.DataAccess.Tests;

[Collection("Database")]
public sealed class ExpenseRepositoryTests
{
    private readonly DatabaseFixture _db;
    private readonly UserRepository _users;
    private readonly TripRepository _trips;
    private readonly ExpenseRepository _expenses;

    public ExpenseRepositoryTests(DatabaseFixture db)
    {
        _db = db;
        _users = new UserRepository(db.Factory);
        _trips = new TripRepository(db.Factory);
        _expenses = new ExpenseRepository(db.Factory);
    }

    [Fact]
    public async Task Add_Then_GetById_Restores_All_Fields_And_Consumers()
    {
        var (trip, payer, consumers) = await SeedAsync();
        var expense = new Expense(
            id:          Guid.NewGuid(),
            tripId:      trip.Id,
            payerId:     payer.Id,
            name:        "Dinner",
            type:        ExpenseType.Food,
            value:       1200m,
            discount:    100m,
            consumerIds: consumers.Select(u => u.Id).ToList());

        await _expenses.AddAsync(expense);
        var fetched = await _expenses.GetByIdAsync(expense.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Dinner", fetched!.Name);
        Assert.Equal(ExpenseType.Food, fetched.Type);
        Assert.Equal(1200m, fetched.Value);
        Assert.Equal(100m, fetched.Discount);
        Assert.Equal(payer.Id, fetched.PayerId);
        Assert.Null(fetched.ReceiptId);
        Assert.Equal(
            consumers.Select(u => u.Id).OrderBy(g => g),
            fetched.ConsumerIds.OrderBy(g => g));
    }

    [Fact]
    public async Task Update_Changes_Fields_And_Rewrites_Consumers()
    {
        var (trip, payer, consumers) = await SeedAsync();
        var expense = new Expense(
            id:          Guid.NewGuid(),
            tripId:      trip.Id,
            payerId:     payer.Id,
            name:        "Taxi",
            type:        ExpenseType.Transport,
            value:       500m,
            discount:    0,
            consumerIds: new[] { consumers[0].Id });
        await _expenses.AddAsync(expense);

        var updated = new Expense(
            id:          expense.Id,
            tripId:      trip.Id,
            payerId:     payer.Id,
            name:        "Taxi (fixed)",
            type:        ExpenseType.Transport,
            value:       800m,
            discount:    50m,
            consumerIds: consumers.Select(u => u.Id).ToList());
        await _expenses.UpdateAsync(updated);

        var fetched = await _expenses.GetByIdAsync(expense.Id);
        Assert.Equal("Taxi (fixed)", fetched!.Name);
        Assert.Equal(800m, fetched.Value);
        Assert.Equal(50m, fetched.Discount);
        Assert.Equal(consumers.Count, fetched.ConsumerIds.Count);
    }

    [Fact]
    public async Task Delete_Removes_Expense_And_Its_Consumers()
    {
        var (trip, payer, consumers) = await SeedAsync();
        var expense = new Expense(
            id:          Guid.NewGuid(),
            tripId:      trip.Id,
            payerId:     payer.Id,
            name:        "Hotel",
            type:        ExpenseType.Accommodation,
            value:       5000m,
            discount:    0,
            consumerIds: consumers.Select(u => u.Id).ToList());
        await _expenses.AddAsync(expense);

        await _expenses.DeleteAsync(expense.Id);

        Assert.Null(await _expenses.GetByIdAsync(expense.Id));
    }

    [Fact]
    public async Task GetByTrip_Returns_All_Expenses_Of_That_Trip()
    {
        var (trip, payer, consumers) = await SeedAsync();
        for (var i = 0; i < 3; i++)
        {
            await _expenses.AddAsync(new Expense(
                id:          Guid.NewGuid(),
                tripId:      trip.Id,
                payerId:     payer.Id,
                name:        $"Item {i}",
                type:        ExpenseType.Other,
                value:       100m + i,
                discount:    0,
                consumerIds: new[] { consumers[0].Id }));
        }

        var list = await _expenses.GetByTripAsync(trip.Id);
        Assert.Equal(3, list.Count);
    }

    private async Task<(Trip trip, User payer, List<User> consumers)> SeedAsync()
    {
        await _db.ResetAsync();
        var sasha = NewUser("sasha");
        var masha = NewUser("masha");
        var dan   = NewUser("dan");
        var egor  = NewUser("egor");
        await _users.AddAsync(sasha);
        await _users.AddAsync(masha);
        await _users.AddAsync(dan);
        await _users.AddAsync(egor);

        var trip = new Trip(Guid.NewGuid(), "Weekend trip", "RUB", DateTime.UtcNow);
        trip.ParticipantIds.Add(sasha.Id);
        trip.ParticipantIds.Add(masha.Id);
        trip.ParticipantIds.Add(dan.Id);
        trip.ParticipantIds.Add(egor.Id);
        await _trips.AddAsync(trip);

        return (trip, sasha, new List<User> { masha, dan });
    }

    private static User NewUser(string tag) => new(
        id:       Guid.NewGuid(),
        name:     $"User {tag}",
        email:    $"{tag}@example.com",
        googleId: $"gid-{tag}");
}
