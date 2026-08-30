using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Session;

// Абстракция текущей сессии Web UI -- аналог IAppSession из ConsoleUI
public interface IWebAppSession
{
    Guid? CurrentUserId { get; }
    Guid? CurrentTripId { get; }
    bool IsAuthenticated { get; }
    bool HasActiveTrip { get; }

    Task<User?> GetCurrentUserAsync();
    Task<Trip?> GetCurrentTripAsync();

    void SignIn(Guid userId);
    void SignOut();
    void SelectTrip(Guid tripId);
    void ClearTrip();
}
