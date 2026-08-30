using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Models;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

// База для контроллеров, которым нужна активная поездка в сессии
public abstract class TripAwareController : Controller
{
    protected IWebAppSession AppSession { get; }

    protected TripAwareController(IWebAppSession session) => AppSession = session;

    // Возвращает текущую поездку либо готовый редирект
    protected async Task<(Trip? trip, IActionResult? redirect)> ResolveCurrentTripAsync()
    {
        if (!AppSession.IsAuthenticated)
            return (null, RedirectToAction("Login", "Account"));

        var trip = await AppSession.GetCurrentTripAsync();
        if (trip is null)
        {
            TempData["Error"] = "Сначала выберите поездку.";
            return (null, RedirectToAction("Index", "Trips"));
        }
        return (trip, null);
    }
}
