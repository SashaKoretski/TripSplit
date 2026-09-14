namespace TripSplit.WebUI.Models;

public class AuthPageVm
{
    public RegisterVm Register { get; set; } = new();
    public LoginByEmailVm LoginByEmail { get; set; } = new();
}
