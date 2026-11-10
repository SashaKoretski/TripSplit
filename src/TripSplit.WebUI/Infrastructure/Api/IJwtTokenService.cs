namespace TripSplit.WebUI.Infrastructure.Api;

public interface IJwtTokenService
{
    // Выпускает access-токен для пользователя; возвращает токен и время жизни в секундах
    (string AccessToken, int ExpiresIn) IssueToken(Guid userId);
}
