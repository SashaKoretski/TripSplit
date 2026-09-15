using System.ComponentModel.DataAnnotations;

namespace TripSplit.WebUI.Models;

public class CreateReceiptVm
{
    [Required(ErrorMessage = "Название обязательно")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата обязательна")]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
