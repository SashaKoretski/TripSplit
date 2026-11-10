namespace TripSplit.WebUI.Infrastructure.Api;

// API-level: регистрация и вход разделены строго (правка WebLab#2 от 2026-10-08),
// в отличие от UserService.RegisterAsync (BR-08), который тихо логинит в существующий
// аккаунт - это поведение Web UI не меняется, API-контроллер проверяет email сам.
public class UserAlreadyRegisteredException : Exception
{
    public UserAlreadyRegisteredException(string email)
        : base($"User with email '{email}' is already registered") { }
}
