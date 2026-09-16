using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.WebUI.Models;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

public class ReceiptsController : TripAwareController
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private readonly IReceiptService _receipts;
    private readonly IReceiptImageService _images;
    private readonly IExpenseService _expenses;

    public ReceiptsController(
        IReceiptService receipts, IReceiptImageService images, IExpenseService expenses, IWebAppSession session)
        : base(session)
    {
        _receipts = receipts;
        _images = images;
        _expenses = expenses;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        var receipts = await _receipts.GetByTripAsync(trip!.Id);
        var expenses = await _expenses.GetByTripAsync(trip.Id);
        var expensesByReceipt = expenses
            .Where(e => e.ReceiptId is not null)
            .ToLookup(e => e.ReceiptId!.Value);

        var rows = new List<ReceiptRowVm>();
        foreach (var r in receipts)
        {
            var image = await _images.GetByReceiptAsync(r.Id);
            rows.Add(new ReceiptRowVm
            {
                Receipt = r,
                ImageUrl = image is null ? null : await _images.GetDownloadUrlAsync(r.Id),
                ImageFileName = image?.OriginalFileName,
                LinkedExpenses = expensesByReceipt[r.Id].ToList()
            });
        }
        return View(new ReceiptListVm { Trip = trip, Receipts = rows });
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

        await _receipts.CreateAsync(trip!.Id, vm.Name.Trim(), vm.Date);
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

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> UploadImage(Guid receiptId, IFormFile file)
    {
        if (!AppSession.IsAuthenticated) return RedirectToAction("Login", "Account");

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Выберите файл.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        await _images.UploadAsync(receiptId, stream, file.FileName, file.ContentType, file.Length);
        TempData["Info"] = "Изображение чека загружено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid receiptId)
    {
        if (!AppSession.IsAuthenticated) return RedirectToAction("Login", "Account");
        await _images.DeleteByReceiptAsync(receiptId);
        TempData["Info"] = "Изображение чека удалено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AttachExpenses(Guid receiptId)
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        var receipt = await _receipts.GetByIdAsync(receiptId);
        var unlinked = (await _expenses.GetByTripAsync(trip!.Id))
            .Where(e => e.ReceiptId is null)
            .ToList();

        return View(new AttachExpensesVm { Trip = trip, Receipt = receipt, UnlinkedExpenses = unlinked });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AttachExpenses(Guid receiptId, List<Guid> expenseIds)
    {
        if (!AppSession.IsAuthenticated) return RedirectToAction("Login", "Account");

        foreach (var expenseId in expenseIds)
            await _expenses.AttachToReceiptAsync(expenseId, receiptId);

        TempData["Info"] = expenseIds.Count == 0 ? "Ничего не выбрано." : "Траты привязаны к чеку.";
        return RedirectToAction(nameof(Index));
    }
}
