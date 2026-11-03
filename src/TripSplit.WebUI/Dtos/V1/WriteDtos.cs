using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/RegisterUserRequest
public class RegisterUserRequestDto
{
    public required string Name { get; init; }
    public required string Email { get; init; }
}

// components/schemas/CreateTripRequest
public class CreateTripRequestDto
{
    public required string Name { get; init; }
    public required string Currency { get; init; }
}

// components/schemas/AddParticipantRequest - пустое тело = self-join, email = invite
public class AddParticipantRequestDto
{
    public string? Email { get; init; }
}

// components/schemas/CreateExpenseRequest - используется и для PUT (replaceExpense)
public class CreateExpenseRequestDto
{
    public required string Name { get; init; }
    public required ExpenseType Type { get; init; }
    public decimal Value { get; init; }
    public decimal Discount { get; init; }
    public required Guid PayerId { get; init; }
    public required IReadOnlyList<Guid> ConsumerIds { get; init; }
    public Guid? ReceiptId { get; init; }
}

// components/schemas/CreateReceiptRequest
public class CreateReceiptRequestDto
{
    public required string Name { get; init; }
    public required DateOnly Date { get; init; }
}
