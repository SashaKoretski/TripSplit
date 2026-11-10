namespace TripSplit.WebUI.Infrastructure.Api;

// Пользователь аутентифицирован, но не состоит в поездке. Проверка членства - API-level
// concern (целевое поведение из WebLab#2), в BusinessLogic такого исключения нет и не нужно:
// Trip.ParticipantIds уже публичны на domain-модели, проверка - тонкая, без изменения BL.
public class ForbiddenException : Exception
{
    public ForbiddenException(Guid tripId)
        : base($"User is not a participant of trip '{tripId}'") { }
}
