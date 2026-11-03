using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TripSplit.BusinessLogic.Interfaces.Services;
using TripSplit.BusinessLogic.Models;
using TripSplit.WebUI.Dtos.V1;
using TripSplit.WebUI.Infrastructure.Api;

namespace TripSplit.WebUI.Controllers.Api.V1;

[Route("api/v1/expenses")]
public class ExpensesController : ApiControllerBase
{
    private readonly IExpenseService _expenses;
    private readonly ITripService _trips;

    public ExpensesController(IExpenseService expenses, ITripService trips)
    {
        _expenses = expenses;
        _trips = trips;
    }

    // operationId: getExpenseById
    [HttpGet("{expenseId:guid}")]
    public async Task<ActionResult<ExpenseDto>> GetById(Guid expenseId)
    {
        var expense = await _expenses.GetByIdAsync(expenseId);
        var trip = await _trips.GetByIdAsync(expense.TripId);
        EnsureParticipant(trip);

        return Ok(ExpenseDto.FromDomain(expense));
    }

    // operationId: replaceExpense - полная замена, перенос в другую поездку запрещен
    [HttpPut("{expenseId:guid}")]
    public async Task<ActionResult<ExpenseDto>> Replace(Guid expenseId, CreateExpenseRequestDto request)
    {
        var existing = await _expenses.GetByIdAsync(expenseId);
        var trip = await _trips.GetByIdAsync(existing.TripId);
        EnsureParticipant(trip);

        var replacement = new Expense(
            existing.Id, existing.TripId, request.PayerId, request.Name, request.Type,
            request.Value, request.Discount, request.ConsumerIds, request.ReceiptId);
        await _expenses.UpdateAsync(replacement);

        var fresh = await _expenses.GetByIdAsync(expenseId);
        return Ok(ExpenseDto.FromDomain(fresh));
    }

    // operationId: patchExpense - JSON Merge Patch (RFC 7396): отсутствующий ключ - не менять,
    // receiptId: null - явно отвязать от чека, null у остальных полей недопустим (нет пустого состояния).
    [HttpPatch("{expenseId:guid}")]
    public async Task<ActionResult<ExpenseDto>> Patch(Guid expenseId, [FromBody] JsonElement body)
    {
        var existing = await _expenses.GetByIdAsync(expenseId);
        var trip = await _trips.GetByIdAsync(existing.TripId);
        EnsureParticipant(trip);

        var name = existing.Name;
        var type = existing.Type;
        var value = existing.Value;
        var discount = existing.Discount;
        var payerId = existing.PayerId;
        IEnumerable<Guid> consumerIds = existing.ConsumerIds;
        var receiptId = existing.ReceiptId;

        if (MergePatch.TryGetField(body, "name", out var nameField))
        {
            MergePatch.EnsureNotNull(nameField, "name");
            name = nameField.GetString()!;
        }
        if (MergePatch.TryGetField(body, "type", out var typeField))
        {
            MergePatch.EnsureNotNull(typeField, "type");
            type = Enum.Parse<ExpenseType>(typeField.GetString()!, ignoreCase: true);
        }
        if (MergePatch.TryGetField(body, "value", out var valueField))
        {
            MergePatch.EnsureNotNull(valueField, "value");
            value = valueField.GetDecimal();
        }
        if (MergePatch.TryGetField(body, "discount", out var discountField))
        {
            MergePatch.EnsureNotNull(discountField, "discount");
            discount = discountField.GetDecimal();
        }
        if (MergePatch.TryGetField(body, "payerId", out var payerField))
        {
            MergePatch.EnsureNotNull(payerField, "payerId");
            payerId = payerField.GetGuid();
        }
        if (MergePatch.TryGetField(body, "consumerIds", out var consumersField))
        {
            MergePatch.EnsureNotNull(consumersField, "consumerIds");
            consumerIds = consumersField.EnumerateArray().Select(e => e.GetGuid()).ToList();
        }
        if (MergePatch.TryGetField(body, "receiptId", out var receiptField))
        {
            receiptId = receiptField.ValueKind == JsonValueKind.Null ? null : receiptField.GetGuid();
        }

        var patched = new Expense(
            existing.Id, existing.TripId, payerId, name, type, value, discount, consumerIds, receiptId);
        await _expenses.UpdateAsync(patched);

        var fresh = await _expenses.GetByIdAsync(expenseId);
        return Ok(ExpenseDto.FromDomain(fresh));
    }

    // operationId: deleteExpense - повторный вызов на уже удаленной трате -> 404
    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> Delete(Guid expenseId)
    {
        var existing = await _expenses.GetByIdAsync(expenseId);
        var trip = await _trips.GetByIdAsync(existing.TripId);
        EnsureParticipant(trip);

        await _expenses.DeleteAsync(expenseId);
        return NoContent();
    }
}
