namespace TripSplit.ConsoleUI.Commands;

// удаление траты по выбору из списка
public sealed class DeleteExpenseCommand : IMenuItem
{
    private readonly IExpenseService _expenses;
    private readonly IAppSession _session;
    private readonly IConsoleIO _io;

    public DeleteExpenseCommand(IExpenseService expenses, IAppSession session, IConsoleIO io)
    {
        _expenses = expenses;
        _session = session;
        _io = io;
    }

    public string Title => "Удалить трату";
    public bool IsAvailable(IAppSession session) =>
        session.HasActiveTrip && session.CurrentTrip!.Status == TripStatus.Active;

    public async Task ExecuteAsync()
    {
        var list = await _expenses.GetByTripAsync(_session.CurrentTrip!.Id);
        if (list.Count == 0)
        {
            _io.WriteLine("Трат нет.");
            return;
        }

        for (int i = 0; i < list.Count; i++)
            _io.WriteLine($"  {i + 1}. {list[i].Name}  {list[i].EffectiveAmount}  [{list[i].Id}]");

        var raw = _io.ReadLine("Номер для удаления: ").Trim();
        if (!int.TryParse(raw, out var idx) || idx < 1 || idx > list.Count)
        {
            _io.WriteLine("Неверный ввод.");
            return;
        }

        await _expenses.DeleteAsync(list[idx - 1].Id);
        _io.WriteLine("Удалено.");
    }
}
