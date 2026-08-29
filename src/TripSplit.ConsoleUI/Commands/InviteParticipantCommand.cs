namespace TripSplit.ConsoleUI.Commands;

// (организатор): добавляет участника по его userId
public sealed class InviteParticipantCommand : IMenuItem
{
    private readonly ITripService _trips;
    private readonly IUserService _users;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public InviteParticipantCommand(
        ITripService trips, IUserService users, IAppSession session, IConsoleIO io)
    {
        _trips = trips;
        _users = users;
        _session = session;
        _io = io;
    }

    public string Title => "Пригласить участника (по userId)";
    public bool IsAvailable(IAppSession session) =>
        session.IsAuthenticated
        && session.HasActiveTrip
        && session.CurrentTrip!.Status == TripStatus.Active;

    public async Task ExecuteAsync()
    {
        var trip = _session.CurrentTrip!;
        _io.WriteLine($"Ссылка-приглашение: trip://{trip.Id}");

        var raw = _io.ReadLine("UserId участника: ").Trim();
        if (!Guid.TryParse(raw, out var userId))
        {
            _io.WriteLine("Неверный Guid.");
            return;
        }

        if (trip.ParticipantIds.Contains(userId))
        {
            _io.WriteLine("Этот пользователь уже в поездке.");
            return;
        }

        var user = await _users.GetByIdAsync(userId);
        await _trips.AddParticipantAsync(trip.Id, userId);
        _session.CurrentTrip = await _trips.GetByIdAsync(trip.Id);

        _io.WriteLine($"Добавлен: {user.Name} <{user.Email}>");
    }
}
