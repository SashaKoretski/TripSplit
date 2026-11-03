namespace TripSplit.WebUI.Configuration;

public class JwtOptions
{
    public string SigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiresInSeconds { get; init; } = 3600;
}
