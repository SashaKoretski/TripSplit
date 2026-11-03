namespace TripSplit.WebUI.Infrastructure.Api;

// Соответствует components/schemas/Error в src/openapi/openapi.yaml (RFC 7807 + стабильный code)
public class ApiError
{
    public string Type { get; init; } = "about:blank";
    public required string Title { get; init; }
    public required int Status { get; init; }
    public string? Detail { get; init; }
    public required string Code { get; init; }
}
