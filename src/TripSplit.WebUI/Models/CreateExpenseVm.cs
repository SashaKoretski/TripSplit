using System.ComponentModel.DataAnnotations;
using TripSplit.BusinessLogic.Models;

namespace TripSplit.WebUI.Models;

public class CreateExpenseVm
{
    [Required(ErrorMessage = "Название обязательно")]
    public string Name { get; set; } = string.Empty;

    public ExpenseType Type { get; set; } = ExpenseType.Other;

    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть положительной")]
    public decimal Value { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Скидка не может быть отрицательной")]
    public decimal Discount { get; set; }

    [Required(ErrorMessage = "Плательщик обязателен")]
    public Guid PayerId { get; set; }

    // Список валидируется вручную в контроллере
    public List<Guid> ConsumerIds { get; set; } = new();

    public Guid? ReceiptId { get; set; }

    // Данные для отрисовки селектов/чекбоксов
    public IReadOnlyList<User> Participants { get; set; } = Array.Empty<User>();
    public IReadOnlyList<Receipt> Receipts { get; set; } = Array.Empty<Receipt>();
}
