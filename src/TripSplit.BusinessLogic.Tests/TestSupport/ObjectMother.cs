using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.TestSupport;

/// <summary>
/// Object Mother / Fabric (ЛР1 Т7): готовые, именованные "жизненные" сценарии для тестов,
/// которым нужен не абстрактный объект, а узнаваемая ситуация ("активная поездка с тремя
/// участниками", "уже завершенная поездка"). В отличие от Builder (гибкая сборка по частям),
/// здесь фиксированы целые сценарии, которые повторяются в разных тестах.
/// </summary>
public static class ObjectMother
{
    public static User Sasha() => new UserBuilder()
        .WithName("Sasha").WithEmail("sasha@example.test").WithGoogleId("gid-sasha").Build();

    public static User Masha() => new UserBuilder()
        .WithName("Masha").WithEmail("masha@example.test").WithGoogleId("gid-masha").Build();

    public static User Misha() => new UserBuilder()
        .WithName("Misha").WithEmail("misha@example.test").WithGoogleId("gid-misha").Build();

    /// <summary>Активная поездка с тремя участниками (Sasha, Masha, Misha) — типовой "happy path".</summary>
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

    /// <summary>Поездка, которая уже завершена — для проверки запрета изменений (Т.AlreadyFinished).</summary>
    public static Trip FinishedTrip() => new TripBuilder().Finished().Build();

    /// <summary>Типовая трата на двоих участников без скидки и без чека.</summary>
    public static Expense DinnerForTwo(Guid tripId, Guid payerId, Guid consumerId) =>
        new ExpenseBuilder()
            .ForTrip(tripId)
            .PaidBy(payerId)
            .WithName("Dinner")
            .ConsumedBy(payerId, consumerId)
            .Build();
}
