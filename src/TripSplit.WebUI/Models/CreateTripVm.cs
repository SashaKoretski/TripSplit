using System.ComponentModel.DataAnnotations;

namespace TripSplit.WebUI.Models;

public class CreateTripVm
{
    [Required(ErrorMessage = "Название обязательно")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Валюта обязательна")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Валюта (3 буквы)")]
    public string Currency { get; set; } = "RUB";
}
