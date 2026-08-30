using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.WebUI.Models;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

public class ReceiptsController : TripAwareController
{
    private readonly IReceiptService _receipts;

    public ReceiptsController(IReceiptService receipts, IWebAppSession session) : base(session)
    {
        _receipts = receipts;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        var receipts = await _receipts.GetByTripAsync(trip!.Id);
        return View(new ReceiptListVm { Trip = trip, Receipts = receipts });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        if (trip!.Status == TripStatus.Finished)
        {
            TempData["Error"] = "Поездка уже завершена, добавлять чеки нельзя.";
            return RedirectToAction(nameof(Index));
        }
        return View(new CreateReceiptVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReceiptVm vm)
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        if (!ModelState.IsValid) return View(vm);

        await _receipts.CreateAsync(trip!.Id, vm.FileUrl.Trim(), vm.Date);
        TempData["Info"] = "Чек добавлен.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!AppSession.IsAuthenticated) return RedirectToAction("Login", "Account");
        await _receipts.DeleteAsync(id);
        TempData["Info"] = "Чек удален.";
        return RedirectToAction(nameof(Index));
    }
}
