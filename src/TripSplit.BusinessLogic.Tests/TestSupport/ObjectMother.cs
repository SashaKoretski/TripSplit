using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.TestSupport;

public static class ObjectMother
{
    public static User Sasha() => new UserBuilder()
        .WithName("Sasha").WithEmail("sasha@example.test").WithGoogleId("gid-sasha").Build();

    public static User Masha() => new UserBuilder()
        .WithName("Masha").WithEmail("masha@example.test").WithGoogleId("gid-masha").Build();

    public static User Misha() => new UserBuilder()
        .WithName("Misha").WithEmail("misha@example.test").WithGoogleId("gid-misha").Build();

    public static Trip ActiveTripWithThreeParticipants()
    {
        var sasha = Sasha();
        var masha = Masha();
        var misha = Misha();
        return new TripBuilder()
            .WithName("Bali 2026")
            .WithParticipants(sasha.Id, masha.Id, misha.Id)
            .Build();
    }

    public static Trip FinishedTrip() => new TripBuilder().Finished().Build();

    public static Expense DinnerForTwo(Guid tripId, Guid payerId, Guid consumerId) =>
        new ExpenseBuilder()
            .ForTrip(tripId)
            .PaidBy(payerId)
            .WithName("Dinner")
            .ConsumedBy(payerId, consumerId)
            .Build();
}
