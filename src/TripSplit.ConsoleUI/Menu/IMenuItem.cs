namespace TripSplit.ConsoleUI.Menu;

public interface IMenuItem
{
    string Title { get; }
    bool IsAvailable(IAppSession session);
    Task ExecuteAsync();
}
