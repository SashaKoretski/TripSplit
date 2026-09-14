using System.ComponentModel.DataAnnotations;

namespace TripSplit.WebUI.Models;

public class LoginByEmailVm
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;
}
