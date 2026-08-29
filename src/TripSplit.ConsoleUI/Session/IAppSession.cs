namespace TripSplit.ConsoleUI.Session;

// Текущее состояние UI: залогиненный пользователь и активная поездка
public interface IAppSession
{
    User? CurrentUser { get; set; }
    Trip? CurrentTrip { get; set; }
    bool IsAuthenticated { get; }
    bool HasActiveTrip { get; }
}
