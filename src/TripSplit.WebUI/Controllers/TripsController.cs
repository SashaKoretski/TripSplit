using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.WebUI.Models;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

public class TripsController : Controller
{
    private readonly ITripService _trips;
    private readonly IUserService _users;
    private readonly IWebAppSession _session;

    public TripsController(ITripService trips, IUserService users, IWebAppSession session)
    {
        _trips = trips;
        _users = users;
        _session = session;
    }

    // список поездок пользователя + формы создания и присоединения
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (_session.CurrentUserId is not { } userId) return RedirectToAction("Login", "Account");

        var trips = await _trips.GetByUserAsync(userId);
        return View(new TripListVm
        {
            Trips = trips,
            CurrentTripId = _session.CurrentTripId
        });
    }

    // создание новой поездки
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Create")] CreateTripVm vm)
    {
        if (_session.CurrentUserId is not { } userId) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return await RerenderIndex(userId, create: vm);

        var trip = await _trips.CreateAsync(vm.Name.Trim(), vm.Currency.Trim().ToUpperInvariant(), userId);
        _session.SelectTrip(trip.Id);
        TempData["Info"] = $"Поездка «{trip.Name}» создана.";
        return RedirectToAction(nameof(Details), new { id = trip.Id });
    }

    // войти в существующую поездку по ее GUID
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Join([Bind(Prefix = "Join")] JoinTripVm vm)
    {
        if (_session.CurrentUserId is not { } userId) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return await RerenderIndex(userId, join: vm);

        await _trips.AddParticipantAsync(vm.TripId, userId);
        _session.SelectTrip(vm.TripId);
        TempData["Info"] = "Вы присоединились к поездке.";
        return RedirectToAction(nameof(Details), new { id = vm.TripId });
    }

    // сделать поездку текущей
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Select(Guid id)
    {
        if (!_session.IsAuthenticated) return RedirectToAction("Login", "Account");
        _session.SelectTrip(id);
        return RedirectToAction(nameof(Details), new { id });
    }

    // снять выбор
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Deselect()
    {
        _session.ClearTrip();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        if (!_session.IsAuthenticated) return RedirectToAction("Login", "Account");

        var trip = await _trips.GetByIdAsync(id);
        var participants = new List<User>(trip.ParticipantIds.Count);
        foreach (var pid in trip.ParticipantIds)
            participants.Add(await _users.GetByIdAsync(pid));

        return View(new TripDetailsVm
        {
            Trip = trip,
            Participants = participants,
            IsSelected = _session.CurrentTripId == id
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(Guid id, [Bind(Prefix = "Invite")] InviteParticipantVm vm)
    {
        if (!_session.IsAuthenticated) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Некорректный email.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var googleId = $"local:{vm.Email.Trim().ToLowerInvariant()}";
        var invitee = await _users.FindByGoogleIdAsync(googleId);
        if (invitee is null)
        {
            TempData["Error"] = "Пользователь с таким email не зарегистрирован. Попросите его сначала войти в TripSplit.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await _trips.AddParticipantAsync(id, invitee.Id);
        TempData["Info"] = $"{invitee.Name} добавлен в поездку.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(Guid id)
    {
        if (!_session.IsAuthenticated) return RedirectToAction("Login", "Account");
        await _trips.FinishAsync(id);
        _session.SelectTrip(id);
        TempData["Info"] = "Поездка завершена. Итоговый расчет готов.";
        return RedirectToAction("Index", "Settlement");
    }

    // Перерисовывает Index с сохраненными данными формы после ошибок валидации
    private async Task<IActionResult> RerenderIndex(Guid userId, CreateTripVm? create = null, JoinTripVm? join = null)
    {
        var trips = await _trips.GetByUserAsync(userId);
        return View(nameof(Index), new TripListVm
        {
            Trips = trips,
            CurrentTripId = _session.CurrentTripId,
            Create = create ?? new(),
            Join = join ?? new()
        });
    }
}
