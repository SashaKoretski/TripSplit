namespace TripSplit.WebUI.Dtos.V1;

// components/schemas/AuthTokenRequest
public class AuthTokenRequestDto
{
    public required string Email { get; init; }
}

// components/schemas/AuthToken
public class AuthTokenDto
{
    public required string AccessToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public required int ExpiresIn { get; init; }
}
