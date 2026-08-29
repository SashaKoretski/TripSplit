namespace TripSplit.ConsoleUI.Commands;

// план минимальных переводов, полученный от стратегии
public sealed class ViewDebtsCommand : IMenuItem
{
    private readonly IDebtSettlementService _settlement;
    private readonly IUserService _users;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public ViewDebtsCommand(
        IDebtSettlementService settlement, IUserService users, IAppSession session, IConsoleIO io)
    {
        _settlement = settlement;
        _users = users;
        _session = session;
        _io = io;
    }

    public string Title => "Посмотреть распределение долгов";
    public bool IsAvailable(IAppSession session) => session.HasActiveTrip;

    public async Task ExecuteAsync()
    {
        var trip = _session.CurrentTrip!;
        var stats = await _settlement.CalculateSettlementAsync(trip.Id);

        if (stats.Transfers.Count == 0)
        {
            _io.WriteLine("Долгов нет — все квиты.");
            return;
        }

        _io.WriteLine("Переводы для погашения:");
        foreach (var t in stats.Transfers)
        {
            var from = await _users.GetByIdAsync(t.FromUserId);
            var to = await _users.GetByIdAsync(t.ToUserId);
            _io.WriteLine($"  {from.Name} → {to.Name}: {t.Amount} {trip.Currency}");
        }
    }
}
