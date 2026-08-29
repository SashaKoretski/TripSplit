namespace TripSplit.ConsoleUI.Commands;

// список трат активной поездки
public sealed class ViewExpensesCommand : IMenuItem
{
    private readonly IExpenseService _expenses;
    private readonly IUserService _users;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public ViewExpensesCommand(
        IExpenseService expenses, IUserService users, IAppSession session, IConsoleIO io)
    {
        _expenses = expenses;
        _users = users;
        _session = session;
        _io = io;
    }

    public string Title => "Посмотреть траты";
    public bool IsAvailable(IAppSession session) => session.HasActiveTrip;

    public async Task ExecuteAsync()
    {
        var trip = _session.CurrentTrip!;
        var list = await _expenses.GetByTripAsync(trip.Id);
        if (list.Count == 0)
        {
            _io.WriteLine("Трат пока нет.");
            return;
        }

        var names = await ResolveNamesAsync(list);

        foreach (var e in list)
        {
            var consumers = string.Join(", ", e.ConsumerIds.Select(id => names[id]));
            _io.WriteLine(
                $"- [{e.Type}] {e.Name}: {e.EffectiveAmount} {trip.Currency} " +
                $"(payer: {names[e.PayerId]}; consumers: {consumers})" +
                (e.ReceiptId is null ? "" : $"  receipt:{e.ReceiptId}"));
            _io.WriteLine($"    id: {e.Id}");
        }
    }

    private async Task<Dictionary<Guid, string>> ResolveNamesAsync(IEnumerable<Expense> list)
    {
        var ids = list.SelectMany(e => e.ConsumerIds.Append(e.PayerId)).Distinct();
        var dict = new Dictionary<Guid, string>();
        foreach (var id in ids)
        {
            var u = await _users.GetByIdAsync(id);
            dict[id] = u.Name;
        }
        return dict;
    }
}
