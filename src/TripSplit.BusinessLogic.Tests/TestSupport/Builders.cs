using TripSplit.BusinessLogic.Models;

namespace TripSplit.BusinessLogic.Tests.TestSupport;

/// <summary>
/// Data Builder (ЛР1 Т7): пошаговое, именованное конструирование тестовых объектов с
/// разумными значениями по умолчанию и точечным переопределением только нужных полей.
/// В отличие от ObjectMother (готовые сценарии "из жизни"), Builder — для случаев,
/// когда тесту важна какая-то одна деталь объекта, а остальное неважно.
/// </summary>
public sealed class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Sasha";
    private string _email = "sasha@example.test";
    private string _googleId = "gid-sasha";

    public UserBuilder WithId(Guid id) { _id = id; return this; }
    public UserBuilder WithName(string name) { _name = name; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithGoogleId(string googleId) { _googleId = googleId; return this; }

    public User Build() => new(_id, _name, _email, _googleId);
}

public sealed class TripBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Weekend trip";
    private string _currency = "RUB";
    private DateTime _createdAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private TripStatus _status = TripStatus.Active;
    private readonly List<Guid> _participantIds = new();

    public TripBuilder WithId(Guid id) { _id = id; return this; }
    public TripBuilder WithName(string name) { _name = name; return this; }
    public TripBuilder WithCurrency(string currency) { _currency = currency; return this; }
    public TripBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }
    public TripBuilder WithStatus(TripStatus status) { _status = status; return this; }
    public TripBuilder Finished() { _status = TripStatus.Finished; return this; }
    public TripBuilder WithParticipant(Guid userId) { _participantIds.Add(userId); return this; }
    public TripBuilder WithParticipants(params Guid[] userIds) { _participantIds.AddRange(userIds); return this; }

    public Trip Build()
    {
        var trip = new Trip(_id, _name, _currency, _createdAt) { Status = _status };
        trip.ParticipantIds.AddRange(_participantIds);
        return trip;
    }
}

public sealed class ExpenseBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _tripId = Guid.NewGuid();
    private Guid _payerId = Guid.NewGuid();
    private string _name = "Dinner";
    private ExpenseType _type = ExpenseType.Food;
    private decimal _value = 300m;
    private decimal _discount;
    private List<Guid> _consumerIds = new() { Guid.NewGuid() };
    private Guid? _receiptId;

    public ExpenseBuilder WithId(Guid id) { _id = id; return this; }
    public ExpenseBuilder ForTrip(Guid tripId) { _tripId = tripId; return this; }
    public ExpenseBuilder PaidBy(Guid payerId) { _payerId = payerId; return this; }
    public ExpenseBuilder WithName(string name) { _name = name; return this; }
    public ExpenseBuilder WithType(ExpenseType type) { _type = type; return this; }
    public ExpenseBuilder WithValue(decimal value) { _value = value; return this; }
    public ExpenseBuilder WithDiscount(decimal discount) { _discount = discount; return this; }
    public ExpenseBuilder ConsumedBy(params Guid[] consumerIds) { _consumerIds = consumerIds.ToList(); return this; }
    public ExpenseBuilder WithReceipt(Guid? receiptId) { _receiptId = receiptId; return this; }

    public Expense Build() =>
        new(_id, _tripId, _payerId, _name, _type, _value, _discount, _consumerIds, _receiptId);
}

public sealed class ReceiptBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _tripId = Guid.NewGuid();
    private string _name = "Кафе";
    private DateOnly _date = new(2026, 8, 25);

    public ReceiptBuilder WithId(Guid id) { _id = id; return this; }
    public ReceiptBuilder ForTrip(Guid tripId) { _tripId = tripId; return this; }
    public ReceiptBuilder WithName(string name) { _name = name; return this; }
    public ReceiptBuilder WithDate(DateOnly date) { _date = date; return this; }

    public Receipt Build() => new(_id, _tripId, _name, _date);
}
