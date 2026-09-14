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

    // страница с формой регистрации и формой входа по email
    [HttpGet]
    public IActionResult Login()
    {
        if (_session.IsAuthenticated) return RedirectToAction("Index", "Trips");
        return View(new AuthPageVm());
    }

    // регистрация; если email уже занят — входим в существующий аккаунт под его сохраненным именем
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterVm vm)
    {
        if (!ModelState.IsValid) return View("Login", new AuthPageVm { Register = vm });

        var googleId = $"local:{vm.Email.Trim().ToLowerInvariant()}";
        var user = await _users.RegisterAsync(vm.Name.Trim(), vm.Email.Trim(), googleId);

        _session.SignIn(user.Id);
        TempData["Info"] = $"Добро пожаловать, {user.Name}!";
        return RedirectToAction("Index", "Trips");
    }

    // вход по уже зарегистрированному email, без пароля
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginByEmail([Bind(Prefix = "LoginByEmail")] LoginByEmailVm vm)
    {
        if (!ModelState.IsValid) return View("Login", new AuthPageVm { LoginByEmail = vm });

        var user = await _users.FindByEmailAsync(vm.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError("LoginByEmail.Email", "Аккаунт с такой почтой не найден. Зарегистрируйтесь.");
            return View("Login", new AuthPageVm { LoginByEmail = vm });
        }

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
