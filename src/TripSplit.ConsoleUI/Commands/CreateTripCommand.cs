namespace TripSplit.ConsoleUI.Commands;

// создает поездку и делает ее активной
public sealed class CreateTripCommand : IMenuItem
{
    private readonly ITripService _trips;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public CreateTripCommand(ITripService trips, IAppSession session, IConsoleIO io)
    {
        _trips = trips;
        _session = session;
        _io = io;
    }

    public string Title => "Создать поездку";
    public bool IsAvailable(IAppSession session) => session.IsAuthenticated;

    public async Task ExecuteAsync()
    {
        var name = _io.ReadLine("Название поездки: ").Trim();
        var currency = _io.ReadLine("Валюта (ISO, напр. RUB): ").Trim();

        var trip = await _trips.CreateAsync(name, currency, _session.CurrentUser!.Id);
        _session.CurrentTrip = trip;

        _io.WriteLine($"Поездка создана. Id: {trip.Id}");
        _io.WriteLine($"Ссылка-приглашение (перешлите ее другим участникам): trip://{trip.Id}");
    }
}
