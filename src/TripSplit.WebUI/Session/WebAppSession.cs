using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Session;

public class WebAppSession : IWebAppSession
{
    private const string UserIdKey = "user_id";
    private const string TripIdKey = "trip_id";

    private readonly IHttpContextAccessor _http;
    private readonly IUserService _users;
    private readonly ITripService _trips;

    public WebAppSession(IHttpContextAccessor http, IUserService users, ITripService trips)
    {
        _http = http;
        _users = users;
        _trips = trips;
    }

    private ISession S => _http.HttpContext?.Session
        ?? throw new InvalidOperationException("HttpContext.Session is not available");

    public Guid? CurrentUserId => TryGetGuid(UserIdKey);
    public Guid? CurrentTripId => TryGetGuid(TripIdKey);
    public bool IsAuthenticated => CurrentUserId is not null;
    public bool HasActiveTrip => CurrentTripId is not null;

    public async Task<User?> GetCurrentUserAsync()
        => CurrentUserId is { } id ? await _users.GetByIdAsync(id) : null;

    public async Task<Trip?> GetCurrentTripAsync()
        => CurrentTripId is { } id ? await _trips.GetByIdAsync(id) : null;

    public void SignIn(Guid userId) => S.SetString(UserIdKey, userId.ToString());

    public void SignOut()
    {
        S.Remove(UserIdKey);
        S.Remove(TripIdKey);
    }

    public void SelectTrip(Guid tripId) => S.SetString(TripIdKey, tripId.ToString());
    public void ClearTrip() => S.Remove(TripIdKey);

    private Guid? TryGetGuid(string key)
        => Guid.TryParse(S.GetString(key), out var g) ? g : null;
}
