using System.ComponentModel.DataAnnotations;

namespace TripSplit.WebUI.Models;

public class JoinTripVm
{
    [Required(ErrorMessage = "ID поездки обязателен")]
    public Guid TripId { get; set; }
}
