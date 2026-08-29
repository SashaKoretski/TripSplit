using TripSplit.ConsoleUI.IO;

namespace TripSplit.ConsoleUI.Session;

public sealed class TripSessionRefresher : ISessionRefresher
{
    private readonly ITripService _trips;
    private readonly IDebtSettlementService _settlement;
    private readonly IUserService _users;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public TripSessionRefresher(
        ITripService trips,
        IDebtSettlementService settlement,
        IUserService users,
        IAppSession session,
        IConsoleIO io)
    {
        _trips = trips;
        _settlement = settlement;
        _users = users;
        _session = session;
        _io = io;
    }

    public async Task RefreshAsync()
    {
        if (!_session.HasActiveTrip) return;

        var prev = _session.CurrentTrip!;
        Trip fresh;
        try
        {
            fresh = await _trips.GetByIdAsync(prev.Id);
        }
        catch
        {
            _session.CurrentTrip = null;
            return;
        }

        _session.CurrentTrip = fresh;

        if (prev.Status == TripStatus.Active && fresh.Status == TripStatus.Finished)
            await PrintFinalReportAsync(fresh);
    }

    private async Task PrintFinalReportAsync(Trip trip)
    {
        var stats = await _settlement.CalculateSettlementAsync(trip.Id);

        _io.WriteLine();
        _io.WriteLine($"=== Поездка \"{trip.Name}\" завершена ===");
        _io.WriteLine($"Всего потрачено: {stats.TotalSpent} {trip.Currency}");
        _io.WriteLine("По участникам:");
        foreach (var (uid, spent) in stats.PerUserSpent)
        {
            var u = await _users.GetByIdAsync(uid);
            _io.WriteLine($"  {u.Name,-20} {spent} {trip.Currency}");
        }

        if (stats.Transfers.Count == 0)
        {
            _io.WriteLine("Долгов нет.");
            return;
        }

        _io.WriteLine("Переводы:");
        foreach (var t in stats.Transfers)
        {
            var from = await _users.GetByIdAsync(t.FromUserId);
            var to = await _users.GetByIdAsync(t.ToUserId);
            _io.WriteLine($"  {from.Name} → {to.Name}: {t.Amount} {trip.Currency}");
        }
    }
}
