using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.WebUI.Models;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

public class SettlementController : TripAwareController
{
    private readonly IDebtSettlementService _settlement;
    private readonly IUserService _users;

    public SettlementController(
        IDebtSettlementService settlement,
        IUserService users,
        IWebAppSession session) : base(session)
    {
        _settlement = settlement;
        _users = users;
    }

    // общий итог + переводы для текущей поездки
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        var stats = await _settlement.CalculateSettlementAsync(trip!.Id);

        var names = new Dictionary<Guid, string>();
        foreach (var id in trip.ParticipantIds)
            names[id] = (await _users.GetByIdAsync(id)).Name;

        var perUser = stats.PerUserSpent
            .Select(kv => new UserSpentVm { Name = names.GetValueOrDefault(kv.Key, "?"), Amount = kv.Value })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var transfers = stats.Transfers
            .Select(t => new SettlementRowVm
            {
                FromName = names.GetValueOrDefault(t.FromUserId, "?"),
                ToName = names.GetValueOrDefault(t.ToUserId, "?"),
                Amount = t.Amount
            })
            .ToList();

        return View(new SettlementVm
        {
            Trip = trip,
            Total = stats.TotalSpent,
            PerUser = perUser,
            Transfers = transfers
        });
    }
}
