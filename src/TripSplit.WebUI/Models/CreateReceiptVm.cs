using System.ComponentModel.DataAnnotations;

namespace TripSplit.WebUI.Models;

public class CreateReceiptVm
{
    [Required(ErrorMessage = "URL или описание обязательно")]
    public string FileUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата обязательна")]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
