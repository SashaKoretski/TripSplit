namespace TripSplit.ConsoleUI.Commands;

// выбрать активную поездку из списка своих
public sealed class SelectTripCommand : IMenuItem
{
    private readonly ITripService _trips;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public SelectTripCommand(ITripService trips, IAppSession session, IConsoleIO io)
    {
        _trips = trips;
        _session = session;
        _io = io;
    }

    public string Title => "Выбрать активную поездку";
    public bool IsAvailable(IAppSession session) => session.IsAuthenticated;

    public async Task ExecuteAsync()
    {
        var list = await _trips.GetByUserAsync(_session.CurrentUser!.Id);
        if (list.Count == 0)
        {
            _io.WriteLine("У вас пока нет поездок.");
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            _io.WriteLine($"  {i + 1}. [{t.Status}] {t.Name} ({t.Currency})  {t.Id}");
        }

        var raw = _io.ReadLine("Выбор: ").Trim();
        if (!int.TryParse(raw, out var idx) || idx < 1 || idx > list.Count)
        {
            _io.WriteLine("Неверный ввод.");
            return;
        }

        _session.CurrentTrip = list[idx - 1];
        _io.WriteLine($"Активна: {_session.CurrentTrip.Name}");
    }
}
