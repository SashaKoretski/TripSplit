using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.WebUI.Models;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _users;
    private readonly IWebAppSession _session;

    public AccountController(IUserService users, IWebAppSession session)
    {
        _users = users;
        _session = session;
    }

    // форма входа
    [HttpGet]
    public IActionResult Login()
    {
        if (_session.IsAuthenticated) return RedirectToAction("Index", "Trips");
        return View(new LoginVm());
    }

    // находит по синтезированному GoogleId, либо регистрирует
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var googleId = $"local:{vm.Email.Trim().ToLowerInvariant()}";
        var user = await _users.RegisterAsync(vm.Name.Trim(), vm.Email.Trim(), googleId);

        _session.SignIn(user.Id);
        TempData["Info"] = $"Добро пожаловать, {user.Name}!";
        return RedirectToAction("Index", "Trips");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _session.SignOut();
        return RedirectToAction("Login");
    }
}
