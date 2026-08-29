namespace TripSplit.ConsoleUI.Commands;

// добавляет трату в активную поездку
public sealed class AddExpenseCommand : IMenuItem
{
    private readonly ITripService _trips;
    private readonly IExpenseService _expenses;
    private readonly IReceiptService _receipts;
    private readonly IUserService _users;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public AddExpenseCommand(
        ITripService trips, IExpenseService expenses, IReceiptService receipts,
        IUserService users, IAppSession session, IConsoleIO io)
    {
        _trips = trips;
        _expenses = expenses;
        _receipts = receipts;
        _users = users;
        _session = session;
        _io = io;
    }

    public string Title => "Добавить трату";
    public bool IsAvailable(IAppSession session) =>
        session.HasActiveTrip && session.CurrentTrip!.Status == TripStatus.Active;

    public async Task ExecuteAsync()
    {
        var trip = await _trips.GetByIdAsync(_session.CurrentTrip!.Id);
        _session.CurrentTrip = trip;

        var name = _io.ReadLine("Название траты: ").Trim();
        var type = ReadEnum<ExpenseType>("Тип");
        var value = ReadDecimal("Сумма (Price): ");
        var discount = ReadDecimal("Скидка (0 если нет): ");

        _io.WriteLine("Плательщик (Payer):");
        var payerId = await PickParticipantAsync(trip);

        _io.WriteLine("На кого потрачено (Consumers):");
        var consumerIds = await PickManyParticipantsAsync(trip);
        if (consumerIds.Count == 0)
        {
            _io.WriteLine("Нужен хотя бы один потребитель.");
            return;
        }

        Guid? receiptId = null;
        var rid = _io.ReadLine("ReceiptId (пусто — без чека): ").Trim();
        if (!string.IsNullOrEmpty(rid))
        {
            if (!Guid.TryParse(rid, out var parsed))
            {
                _io.WriteLine("Неверный Guid.");
                return;
            }
            _ = await _receipts.GetByIdAsync(parsed);
            receiptId = parsed;
        }

        var expense = await _expenses.AddAsync(
            trip.Id, payerId, name, type, value, discount, consumerIds, receiptId);

        _io.WriteLine($"Добавлено. Id: {expense.Id}");
    }

    private async Task<Guid> PickParticipantAsync(Trip trip)
    {
        for (int i = 0; i < trip.ParticipantIds.Count; i++)
        {
            var u = await _users.GetByIdAsync(trip.ParticipantIds[i]);
            _io.WriteLine($"  {i + 1}. {u.Name}");
        }
        while (true)
        {
            var raw = _io.ReadLine("Выбор: ").Trim();
            if (int.TryParse(raw, out var idx) && idx >= 1 && idx <= trip.ParticipantIds.Count)
                return trip.ParticipantIds[idx - 1];
            _io.WriteLine("Неверный ввод, повторите.");
        }
    }

    private async Task<List<Guid>> PickManyParticipantsAsync(Trip trip)
    {
        for (int i = 0; i < trip.ParticipantIds.Count; i++)
        {
            var u = await _users.GetByIdAsync(trip.ParticipantIds[i]);
            _io.WriteLine($"  {i + 1}. {u.Name}");
        }
        var raw = _io.ReadLine("Номера через запятую (пусто — все): ").Trim();
        if (string.IsNullOrEmpty(raw))
            return trip.ParticipantIds.ToList();

        var result = new List<Guid>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out var idx) && idx >= 1 && idx <= trip.ParticipantIds.Count)
                result.Add(trip.ParticipantIds[idx - 1]);
        }
        return result.Distinct().ToList();
    }

    private T ReadEnum<T>(string label) where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        for (int i = 0; i < values.Length; i++)
            _io.WriteLine($"  {i + 1}. {values[i]}");
        while (true)
        {
            var raw = _io.ReadLine($"{label}: ").Trim();
            if (int.TryParse(raw, out var idx) && idx >= 1 && idx <= values.Length)
                return values[idx - 1];
            _io.WriteLine("Неверный ввод, повторите.");
        }
    }

    private decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            var raw = _io.ReadLine(prompt).Trim().Replace(',', '.');
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                return value;
            _io.WriteLine("Неверное число, повторите.");
        }
    }
}
