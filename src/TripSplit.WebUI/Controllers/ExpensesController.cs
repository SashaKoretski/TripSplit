using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.WebUI.Models;
using TripSplit.WebUI.Session;

namespace TripSplit.WebUI.Controllers;

public class ExpensesController : TripAwareController
{
    private readonly IExpenseService _expenses;
    private readonly IReceiptService _receipts;
    private readonly IReceiptImageService _receiptImages;
    private readonly IUserService _users;

    public ExpensesController(
        IExpenseService expenses,
        IReceiptService receipts,
        IReceiptImageService receiptImages,
        IUserService users,
        IWebAppSession session) : base(session)
    {
        _expenses = expenses;
        _receipts = receipts;
        _receiptImages = receiptImages;
        _users = users;
    }

    // список трат текущей поездки
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        var expenses = await _expenses.GetByTripAsync(trip!.Id);
        var names = await BuildUserNamesAsync(trip.ParticipantIds);

        var rows = expenses.Select(e => new ExpenseRowVm
        {
            Expense = e,
            PayerName = names.GetValueOrDefault(e.PayerId, "?"),
            ConsumerNames = e.ConsumerIds.Select(id => names.GetValueOrDefault(id, "?")).ToList()
        }).ToList();

        return View(new ExpenseListVm
        {
            Trip = trip,
            Expenses = rows,
            Total = expenses.Sum(e => e.EffectiveAmount)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? receiptId)
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        if (trip!.Status == TripStatus.Finished)
        {
            TempData["Error"] = "Поездка уже завершена, добавлять траты нельзя.";
            return RedirectToAction(nameof(Index));
        }

        var vm = await BuildCreateVmAsync(trip);
        vm.ReceiptId = receiptId;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseVm vm)
    {
        var (trip, redirect) = await ResolveCurrentTripAsync();
        if (redirect is not null) return redirect;

        if (vm.ConsumerIds.Count == 0)
            ModelState.AddModelError(nameof(vm.ConsumerIds), "Выберите хотя бы одного получателя");
        if (vm.Discount > vm.Value)
            ModelState.AddModelError(nameof(vm.Discount), "Скидка не может превышать сумму");

        if (!ModelState.IsValid)
        {
            var repopulated = await BuildCreateVmAsync(trip!);
            repopulated.Name = vm.Name;
            repopulated.Type = vm.Type;
            repopulated.Value = vm.Value;
            repopulated.Discount = vm.Discount;
            repopulated.PayerId = vm.PayerId;
            repopulated.ConsumerIds = vm.ConsumerIds;
            repopulated.ReceiptId = vm.ReceiptId;
            repopulated.NewReceiptName = vm.NewReceiptName;
            repopulated.NewReceiptDate = vm.NewReceiptDate;
            return View(repopulated);
        }

        var receiptId = vm.ReceiptId;
        if (vm.NewReceiptFile is { Length: > 0 } file)
        {
            var receiptName = string.IsNullOrWhiteSpace(vm.NewReceiptName) ? vm.Name : vm.NewReceiptName;
            var receipt = await _receipts.CreateAsync(trip!.Id, receiptName.Trim(), vm.NewReceiptDate);
            await using var stream = file.OpenReadStream();
            await _receiptImages.UploadAsync(receipt.Id, stream, file.FileName, file.ContentType, file.Length);
            receiptId = receipt.Id;
        }

        await _expenses.AddAsync(
            trip!.Id, vm.PayerId, vm.Name.Trim(), vm.Type,
            vm.Value, vm.Discount, vm.ConsumerIds, receiptId);

        TempData["Info"] = "Трата добавлена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!AppSession.IsAuthenticated) return RedirectToAction("Login", "Account");
        await _expenses.DeleteAsync(id);
        TempData["Info"] = "Трата удалена.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<CreateExpenseVm> BuildCreateVmAsync(Trip trip)
    {
        var users = new List<User>(trip.ParticipantIds.Count);
        foreach (var pid in trip.ParticipantIds)
            users.Add(await _users.GetByIdAsync(pid));

        var receipts = await _receipts.GetByTripAsync(trip.Id);

        return new CreateExpenseVm
        {
            Participants = users,
            Receipts = receipts,
            PayerId = AppSession.CurrentUserId ?? (users.Count > 0 ? users[0].Id : Guid.Empty),
            ConsumerIds = trip.ParticipantIds.ToList()   // по умолчанию делим на всех
        };
    }

    private async Task<Dictionary<Guid, string>> BuildUserNamesAsync(IEnumerable<Guid> ids)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var id in ids)
        {
            var u = await _users.GetByIdAsync(id);
            map[id] = u.Name;
        }
        return map;
    }
}
