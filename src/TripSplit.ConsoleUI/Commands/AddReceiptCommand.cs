namespace TripSplit.ConsoleUI.Commands;

// создает чек и опционально привязывает к нему уже добавленные траты
public sealed class AddReceiptCommand : IMenuItem
{
    private readonly IReceiptService _receipts;
    private readonly IExpenseService _expenses;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public AddReceiptCommand(
        IReceiptService receipts, IExpenseService expenses, IAppSession session, IConsoleIO io)
    {
        _receipts = receipts;
        _expenses = expenses;
        _session = session;
        _io = io;
    }

    public string Title => "Добавить чек (и привязать траты)";
    public bool IsAvailable(IAppSession session) =>
        session.HasActiveTrip && session.CurrentTrip!.Status == TripStatus.Active;

    public async Task ExecuteAsync()
    {
        var trip = _session.CurrentTrip!;

        var fileUrl = _io.ReadLine("URL файла чека: ").Trim();
        var dateRaw = _io.ReadLine("Дата (YYYY-MM-DD): ").Trim();
        if (!DateOnly.TryParse(dateRaw, out var date))
        {
            _io.WriteLine("Неверная дата.");
            return;
        }

        var receipt = await _receipts.CreateAsync(trip.Id, fileUrl, date);
        _io.WriteLine($"Чек создан. Id: {receipt.Id}");

        var unlinked = (await _expenses.GetByTripAsync(trip.Id))
            .Where(e => e.ReceiptId is null)
            .ToList();

        if (unlinked.Count == 0)
        {
            _io.WriteLine("Свободных трат для привязки нет.");
            return;
        }

        _io.WriteLine("Непривязанные траты:");
        for (int i = 0; i < unlinked.Count; i++)
            _io.WriteLine($"  {i + 1}. {unlinked[i].Name}  {unlinked[i].EffectiveAmount}");

        var raw = _io.ReadLine("Номера через запятую (пусто — пропустить): ").Trim();
        if (string.IsNullOrEmpty(raw)) return;

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part.Trim(), out var idx) || idx < 1 || idx > unlinked.Count)
                continue;

            var e = unlinked[idx - 1];
            await _expenses.DeleteAsync(e.Id);
            await _expenses.AddAsync(
                e.TripId, e.PayerId, e.Name, e.Type, e.Value, e.Discount,
                e.ConsumerIds, receipt.Id);
        }
        _io.WriteLine("Привязано.");
    }
}
