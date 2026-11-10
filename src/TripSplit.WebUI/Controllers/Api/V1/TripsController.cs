using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models.Exceptions;
using TripSplit.WebUI.Dtos.V1;

namespace TripSplit.WebUI.Controllers.Api.V1;

[Route("api/v1/trips")]
public class TripsController : ApiControllerBase
{
    private readonly ITripService _trips;
    private readonly IUserService _users;
    private readonly IExpenseService _expenses;
    private readonly IReceiptService _receipts;
    private readonly IReceiptImageService _receiptImages;
    private readonly IDebtSettlementService _settlement;

    public TripsController(
        ITripService trips,
        IUserService users,
        IExpenseService expenses,
        IReceiptService receipts,
        IReceiptImageService receiptImages,
        IDebtSettlementService settlement)
    {
        _trips = trips;
        _users = users;
        _expenses = expenses;
        _receipts = receipts;
        _receiptImages = receiptImages;
        _settlement = settlement;
    }

    // operationId: listTrips
    [HttpGet]
    public async Task<ActionResult<PageDto<TripDto>>> List(int page = 1, int pageSize = 20)
    {
        var trips = await _trips.GetByUserAsync(CurrentUserId);
        var dtos = trips.Select(TripDto.FromDomain).ToList();
        return Ok(PageDto<TripDto>.Of(dtos, ClampPage(page), ClampPageSize(pageSize)));
    }

    // operationId: createTrip - создатель автоматически первый участник (FR-004, TripService.CreateAsync)
    [HttpPost]
    public async Task<ActionResult<TripDto>> Create(CreateTripRequestDto request)
    {
        var trip = await _trips.CreateAsync(request.Name, request.Currency, CurrentUserId);
        var dto = TripDto.FromDomain(trip);
        return CreatedAtAction(nameof(GetById), new { tripId = trip.Id }, dto);
    }

    // operationId: getTripById
    [HttpGet("{tripId:guid}")]
    public async Task<ActionResult<TripDto>> GetById(Guid tripId)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);
        return Ok(TripDto.FromDomain(trip));
    }

    // operationId: finishTrip - необратимый переход active -> finished (BR-05)
    [HttpPost("{tripId:guid}/finish")]
    public async Task<ActionResult<TripDto>> Finish(Guid tripId)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);

        await _trips.FinishAsync(tripId);

        var fresh = await _trips.GetByIdAsync(tripId);
        return Ok(TripDto.FromDomain(fresh));
    }

    // operationId: addTripParticipant - self-join (тело {}) не требует предварительного
    // членства (в этом и смысл операции, FR-005); invite-by-email (BR-09) - тоже, как и
    // сейчас в Web UI (TripsController.Invite не проверяет членство приглашающего -
    // известный гэп WebLab#1, API его не ужесточает для этой операции).
    [HttpPost("{tripId:guid}/participants")]
    public async Task<ActionResult<UserDto>> AddParticipant(Guid tripId, AddParticipantRequestDto? request)
    {
        var trip = await _trips.GetByIdAsync(tripId);

        Guid userId;
        if (string.IsNullOrWhiteSpace(request?.Email))
        {
            userId = CurrentUserId;
        }
        else
        {
            var invitee = await _users.FindByEmailAsync(request.Email)
                ?? throw new UserNotFoundException(request.Email);
            userId = invitee.Id;
        }

        var wasParticipant = trip.ParticipantIds.Contains(userId);
        await _trips.AddParticipantAsync(tripId, userId);

        var dto = UserDto.FromDomain(await _users.GetByIdAsync(userId));
        return wasParticipant ? Ok(dto) : StatusCode(StatusCodes.Status201Created, dto);
    }

    // operationId: listTripParticipants
    [HttpGet("{tripId:guid}/participants")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> ListParticipants(Guid tripId)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);

        var users = new List<UserDto>(trip.ParticipantIds.Count);
        foreach (var userId in trip.ParticipantIds)
            users.Add(UserDto.FromDomain(await _users.GetByIdAsync(userId)));

        return Ok(users);
    }

    // operationId: getTripSettlement
    [HttpGet("{tripId:guid}/settlement")]
    public async Task<ActionResult<SettlementDto>> GetSettlement(Guid tripId)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);

        var stats = await _settlement.CalculateSettlementAsync(tripId);
        return Ok(SettlementDto.FromDomain(stats));
    }

    // operationId: listTripExpenses
    [HttpGet("{tripId:guid}/expenses")]
    public async Task<ActionResult<PageDto<ExpenseDto>>> ListExpenses(Guid tripId, int page = 1, int pageSize = 20)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);

        var expenses = await _expenses.GetByTripAsync(tripId);
        var dtos = expenses.Select(ExpenseDto.FromDomain).ToList();
        return Ok(PageDto<ExpenseDto>.Of(dtos, ClampPage(page), ClampPageSize(pageSize)));
    }

    // operationId: createExpense
    [HttpPost("{tripId:guid}/expenses")]
    public async Task<ActionResult<ExpenseDto>> CreateExpense(Guid tripId, CreateExpenseRequestDto request)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);

        var expense = await _expenses.AddAsync(
            tripId, request.PayerId, request.Name, request.Type,
            request.Value, request.Discount, request.ConsumerIds, request.ReceiptId);

        var dto = ExpenseDto.FromDomain(expense);
        return CreatedAtAction("GetById", "Expenses", new { expenseId = expense.Id }, dto);
    }

    // operationId: createReceipt
    [HttpPost("{tripId:guid}/receipts")]
    public async Task<ActionResult<ReceiptDto>> CreateReceipt(Guid tripId, CreateReceiptRequestDto request)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);

        var receipt = await _receipts.CreateAsync(tripId, request.Name, request.Date);

        var dto = await ReceiptDtoFactory.BuildAsync(receipt, _receiptImages);
        return CreatedAtAction("GetById", "Receipts", new { receiptId = receipt.Id }, dto);
    }

    // operationId: listTripReceipts
    [HttpGet("{tripId:guid}/receipts")]
    public async Task<ActionResult<IReadOnlyList<ReceiptDto>>> ListReceipts(Guid tripId)
    {
        var trip = await _trips.GetByIdAsync(tripId);
        EnsureParticipant(trip);

        var receipts = await _receipts.GetByTripAsync(tripId);
        var dtos = new List<ReceiptDto>(receipts.Count);
        foreach (var receipt in receipts)
            dtos.Add(await ReceiptDtoFactory.BuildAsync(receipt, _receiptImages));

        return Ok(dtos);
    }

    private static int ClampPage(int page) => Math.Max(1, page);
    private static int ClampPageSize(int pageSize) => Math.Clamp(pageSize, 1, 100);
}
