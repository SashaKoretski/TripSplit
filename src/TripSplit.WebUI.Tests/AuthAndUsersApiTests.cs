using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace TripSplit.WebUI.Tests;

[Collection("Api")]
public class AuthAndUsersApiTests
{
    private readonly ApiTestFixture _fixture;

    public AuthAndUsersApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RegisterUser_ThenAuthToken_Succeeds()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/v1/users",
            new { name = "Anna", email = "anna@example.com" });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        Assert.NotNull(registerResponse.Headers.Location);

        var tokenResponse = await client.PostAsJsonAsync("/api/v1/auth/token", new { email = "anna@example.com" });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmail_Returns409()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();
        await TestClient.RegisterAsync(client, "Anna", "dup@example.com");

        var resp = await client.PostAsJsonAsync("/api/v1/users", new { name = "Someone Else", email = "dup@example.com" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("user_already_registered", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AuthToken_UnknownEmail_Returns404()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/token", new { email = "nobody@example.com" });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetTripById_WithoutToken_Returns401()
    {
        await _fixture.ResetAsync();
        var client = _fixture.Factory.CreateClient();

        var resp = await client.GetAsync($"/api/v1/trips/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
