namespace TripSplit.ConsoleUI.Commands;

// (участник): войти по ссылке-приглашению trip://<guid>
public sealed class JoinTripCommand : IMenuItem
{
    private readonly ITripService _trips;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public JoinTripCommand(ITripService trips, IAppSession session, IConsoleIO io)
    {
        _trips = trips;
        _session = session;
        _io = io;
    }

    public string Title => "Присоединиться к поездке по ссылке";
    public bool IsAvailable(IAppSession session) => session.IsAuthenticated;

    public async Task ExecuteAsync()
    {
        var raw = _io.ReadLine("Ссылка (trip://<guid>) или голый guid: ").Trim();
        raw = raw.Replace("trip://", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        if (!Guid.TryParse(raw, out var tripId))
        {
            _io.WriteLine("Неверная ссылка.");
            return;
        }

        var trip = await _trips.GetByIdAsync(tripId);
        var me = _session.CurrentUser!.Id;

        if (trip.ParticipantIds.Contains(me))
        {
            _io.WriteLine("Вы уже в этой поездке.");
        }
        else
        {
            await _trips.AddParticipantAsync(trip.Id, me);
            trip = await _trips.GetByIdAsync(tripId);
            _io.WriteLine($"Добавлено. Активна: {trip.Name}");
        }
        _session.CurrentTrip = trip;
    }
}
