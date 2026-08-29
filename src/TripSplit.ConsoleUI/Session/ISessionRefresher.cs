namespace TripSplit.ConsoleUI.Session;

// Синхронизирует IAppSession с БД перед каждой отрисовкой меню
public interface ISessionRefresher
{
    Task RefreshAsync();
}
