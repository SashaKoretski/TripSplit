namespace TripSplit.ConsoleUI.Menu;

public sealed class MenuRunner
{
    private readonly IReadOnlyList<IMenuItem> _items;
    private readonly IConsoleIO _io;
    private readonly IAppSession _session;
    private readonly ISessionRefresher _refresher;

    public MenuRunner(
        IEnumerable<IMenuItem> items,
        IConsoleIO io,
        IAppSession session,
        ISessionRefresher refresher)
    {
        _items = items.ToList();
        _io = io;
        _session = session;
        _refresher = refresher;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            await _refresher.RefreshAsync();

            var available = _items.Where(i => i.IsAvailable(_session)).ToList();

            _io.WriteLine();
            _io.WriteLine("=== TripSplit Console UI ===");
            if (_session.IsAuthenticated)
                _io.WriteLine($"User : {_session.CurrentUser!.Name} <{_session.CurrentUser.Email}>  [{_session.CurrentUser.Id}]");
            if (_session.HasActiveTrip)
                _io.WriteLine($"Trip : {_session.CurrentTrip!.Name} [{_session.CurrentTrip.Status}]  [{_session.CurrentTrip.Id}]");
            _io.WriteLine();

            for (int i = 0; i < available.Count; i++)
                _io.WriteLine($"  {i + 1}. {available[i].Title}");
            _io.WriteLine("  0. Выход");

            var input = _io.ReadLine("> ").Trim();
            if (input == "0") return;

            if (!int.TryParse(input, out var idx) || idx < 1 || idx > available.Count)
            {
                _io.WriteLine("Неверный ввод.");
                continue;
            }

            try
            {
                await available[idx - 1].ExecuteAsync();
            }
            catch (Exception ex)
            {
                _io.WriteLine($"[Ошибка] {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
