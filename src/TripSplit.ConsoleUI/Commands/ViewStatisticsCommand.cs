namespace TripSplit.ConsoleUI.Commands;

// общая сумма и сколько потратил каждый участник
public sealed class ViewStatisticsCommand : IMenuItem
{
    private readonly IDebtSettlementService _settlement;
    private readonly IUserService _users;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public ViewStatisticsCommand(
        IDebtSettlementService settlement, IUserService users, IAppSession session, IConsoleIO io)
    {
        _settlement = settlement;
        _users = users;
        _session = session;
        _io = io;
    }

    public string Title => "Посмотреть статистику";
    public bool IsAvailable(IAppSession session) => session.HasActiveTrip;

    public async Task ExecuteAsync()
    {
        var trip = _session.CurrentTrip!;
        var stats = await _settlement.CalculateSettlementAsync(trip.Id);

        _io.WriteLine($"Всего потрачено: {stats.TotalSpent} {trip.Currency}");
        _io.WriteLine("По участникам:");
        foreach (var (uid, spent) in stats.PerUserSpent)
        {
            var u = await _users.GetByIdAsync(uid);
            _io.WriteLine($"  - {u.Name,-20} {spent} {trip.Currency}");
        }
    }
}
