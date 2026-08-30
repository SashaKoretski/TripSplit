using Microsoft.AspNetCore.Mvc;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

public class HomeController : Controller
{
    private readonly IWebAppSession _session;

    public HomeController(IWebAppSession session) => _session = session;

    public IActionResult Index()
    {
        if (!_session.IsAuthenticated) return RedirectToAction("Login", "Account");
        return RedirectToAction("Index", "Trips");
    }

    public IActionResult Error() => View();
}
