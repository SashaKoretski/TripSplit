namespace TripSplit.ConsoleUI.Commands;

// закрывает активную поездку. Итог и переход в режим просмотра — через SessionRefresher.
public sealed class FinishTripCommand : IMenuItem
{
    private readonly ITripService _trips;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public FinishTripCommand(ITripService trips, IAppSession session, IConsoleIO io)
    {
        _trips = trips;
        _session = session;
        _io = io;
    }

    public string Title => "Закончить поездку";
    public bool IsAvailable(IAppSession session) =>
        session.HasActiveTrip && session.CurrentTrip!.Status == TripStatus.Active;

    public async Task ExecuteAsync()
    {
        var trip = _session.CurrentTrip!;
        if (_io.ReadLine($"Точно закончить \"{trip.Name}\"? (y/n): ").Trim().ToLower() != "y")
            return;

        await _trips.FinishAsync(trip.Id);
        _io.WriteLine("Поездка завершена.");
    }
}
