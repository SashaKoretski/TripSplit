namespace TripSplit.ConsoleUI.Session;

public sealed class AppSession : IAppSession
{
    public User? CurrentUser { get; set; }
    public Trip? CurrentTrip { get; set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public bool HasActiveTrip => CurrentTrip is not null;
}
